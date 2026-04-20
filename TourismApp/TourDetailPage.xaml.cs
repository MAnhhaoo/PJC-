using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Plugin.Maui.Audio;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

[QueryProperty(nameof(TourIdStr), "tourId")]
public partial class TourDetailPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private readonly GpsService _gpsService;
    private readonly LanguageService _langService;
    private readonly OfflineSyncService _offlineSyncService;
    private readonly TranslationService _translationService;

    private Tour? _tour;
    private int _currentIndex = 0;
    private IAudioPlayer? _activePlayer;
    private bool _isSpeaking;
    private CancellationTokenSource? _ttsCts;
    private CancellationTokenSource? _audioCts;
    private bool _isMonitoring;
    private readonly HashSet<int> _autoPlayedPOIs = new();
    private readonly string _sessionId = Guid.NewGuid().ToString();
    private bool _isPurchased;

    public string TourIdStr
    {
        set
        {
            if (int.TryParse(value, out var id))
                _tourId = id;
        }
    }
    private int _tourId;

    public TourDetailPage(HttpClient httpClient, IAudioManager audioManager, GpsService gpsService, LanguageService languageService, OfflineSyncService offlineSyncService, TranslationService translationService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _audioManager = audioManager;
        _gpsService = gpsService;
        _langService = languageService;
        _offlineSyncService = offlineSyncService;
        _translationService = translationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_tourId > 0)
            await LoadTour();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isMonitoring = false;
        StopAudio();
    }

    private async Task LoadTour()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    _tour = await _httpClient.GetFromJsonAsync<Tour>($"api/tours/{_tourId}");
                }
                catch
                {
                    _tour = await _offlineSyncService.GetTourByIdOfflineAsync(_tourId);
                }
            }
            else
            {
                _tour = await _offlineSyncService.GetTourByIdOfflineAsync(_tourId);
            }

            if (_tour == null || !_tour.POIs.Any())
            {
                await DisplayAlert(_langService["Error"], _langService["TourNotFound"], _langService["OK"]);
                return;
            }

            _tour.POIs = _tour.POIs.OrderBy(p => p.OrderIndex).ToList();

            // Translate tour and POI data if language is not Vietnamese
            var currentLang = (_langService.CurrentLanguage ?? "vi").Trim().ToLower();
            if (currentLang != "vi")
            {
                _tour.Name = await _translationService.TranslateAsync(_tour.Name, currentLang);
                _tour.Description = await _translationService.TranslateAsync(_tour.Description, currentLang);
                foreach (var poi in _tour.POIs)
                {
                    poi.RestaurantName = await _translationService.TranslateAsync(poi.RestaurantName, currentLang);
                    if (!string.IsNullOrEmpty(poi.RestaurantAddress))
                        poi.RestaurantAddress = await _translationService.TranslateAsync(poi.RestaurantAddress, currentLang);
                }
            }

            // Resolve image URLs
            var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp;
            foreach (var poi in _tour.POIs)
            {
                if (!string.IsNullOrEmpty(poi.RestaurantImage))
                {
                    if (!poi.RestaurantImage.StartsWith("http"))
                        poi.RestaurantImage = $"http://{host}:5216/{poi.RestaurantImage.TrimStart('/')}";
                }
            }

            Title = _langService["TourJourney"];
            lblTourName.Text = _tour.Name;
            lblTourDesc.Text = _tour.Description;
            lblPOICount.Text = string.Format(_langService["TotalPoints"], _tour.POIs.Count);

            // Show price
            if (_tour.Price > 0)
                lblTourPrice.Text = string.Format(_langService["PriceTag"], _tour.Price.ToString("N0"));
            else
                lblTourPrice.Text = _langService["FreeTag"];

            // Check purchase status
            _isPurchased = _tour.IsPurchased;
            if (_tour.Price > 0 && !_isPurchased)
            {
                paymentBanner.IsVisible = true;
                lblPaymentInfo.Text = string.Format(_langService["NeedPaymentPrice"], _tour.Price.ToString("N0"));
                btnBuyTour.Text = string.Format(_langService["BuyTourPrice"], _tour.Price.ToString("N0"));
                await CheckPaymentStatusAsync();
            }
            else
            {
                paymentBanner.IsVisible = false;
            }

            _currentIndex = 0;
            ShowCurrentPOI();
            DrawRoute();
            StartGpsMonitoring();
        }
        catch (Exception ex)
        {
            await DisplayAlert(_langService["Error"], string.Format(_langService["TourLoadError"], ex.Message), _langService["OK"]);
        }
    }

    private void ShowCurrentPOI()
    {
        if (_tour == null || !_tour.POIs.Any()) return;

        var poi = _tour.POIs[_currentIndex];

        lblCurrentPOI.Text = string.Format(_langService["PointOf"], _currentIndex + 1, _tour.POIs.Count);
        lblPOIOrder.Text = string.Format(_langService["PointLabel"], poi.OrderIndex);
        lblPOIName.Text = poi.RestaurantName;
        lblPOIAddress.Text = poi.RestaurantAddress ?? "";

        if (!string.IsNullOrEmpty(poi.RestaurantImage))
            imgPOI.Source = ImageSource.FromUri(new Uri(poi.RestaurantImage));
        else
            imgPOI.Source = null;

        btnPrev.IsEnabled = _currentIndex > 0;
        btnNext.IsEnabled = _currentIndex < _tour.POIs.Count - 1;

        // Center map on current POI
        if (IsValidCoordinates(poi.Latitude, poi.Longitude))
        {
            var pos = new Location(poi.Latitude, poi.Longitude);
            TourMap.MoveToRegion(MapSpan.FromCenterAndRadius(pos, Distance.FromKilometers(0.5)));
        }
    }

    private void DrawRoute()
    {
        if (_tour == null) return;

        TourMap.Pins.Clear();

        // Add pins for all POIs
        foreach (var poi in _tour.POIs)
        {
            if (!IsValidCoordinates(poi.Latitude, poi.Longitude)) continue;

            TourMap.Pins.Add(new Pin
            {
                Label = $"{poi.OrderIndex}. {poi.RestaurantName}",
                Address = poi.RestaurantAddress ?? "",
                Location = new Location(poi.Latitude, poi.Longitude),
                Type = PinType.Place
            });
        }

        // Draw polyline connecting POIs
        var validPOIs = _tour.POIs.Where(p => IsValidCoordinates(p.Latitude, p.Longitude)).ToList();
        if (validPOIs.Count >= 2)
        {
            var polyline = new Polyline
            {
                StrokeColor = Color.FromArgb("#1565C0"),
                StrokeWidth = 5
            };

            foreach (var poi in validPOIs)
                polyline.Geopath.Add(new Location(poi.Latitude, poi.Longitude));

            TourMap.MapElements.Add(polyline);
        }

        // Fit map to show all POIs
        if (validPOIs.Any())
        {
            var minLat = validPOIs.Min(p => p.Latitude);
            var maxLat = validPOIs.Max(p => p.Latitude);
            var minLng = validPOIs.Min(p => p.Longitude);
            var maxLng = validPOIs.Max(p => p.Longitude);

            var center = new Location((minLat + maxLat) / 2, (minLng + maxLng) / 2);
            var latDelta = Math.Max((maxLat - minLat) * 1.3, 0.01);
            var lngDelta = Math.Max((maxLng - minLng) * 1.3, 0.01);
            var radius = Distance.FromKilometers(Math.Max(latDelta, lngDelta) * 111 / 2);

            TourMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, radius));
        }
    }

    private void OnPrevClicked(object sender, EventArgs e)
    {
        if (_currentIndex > 0)
        {
            StopAudio();
            _currentIndex--;
            ShowCurrentPOI();
        }
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        if (_tour != null && _currentIndex < _tour.POIs.Count - 1)
        {
            StopAudio();
            _currentIndex++;
            ShowCurrentPOI();
        }
    }

    private async void OnPlayNarration(object sender, EventArgs e)
    {
        if (_tour == null) return;

        // Check if tour requires payment
        if (_tour.Price > 0 && !_isPurchased)
        {
            await DisplayAlert(_langService["NeedPayment"],
                string.Format(_langService["NeedPaymentMsg"], _tour.Price.ToString("N0")),
                _langService["OK"]);
            return;
        }

        var poi = _tour.POIs[_currentIndex];
        await PlayPOINarration(poi);
    }

    private void OnStopNarration(object sender, EventArgs e)
    {
        StopAudio();
    }

    private async Task PlayPOINarration(TourPOI poi)
    {
        StopAudio();

        if (poi.Narrations == null || !poi.Narrations.Any())
        {
            await DisplayAlert(_langService["Notification"], string.Format(_langService["NoNarrationPOI"], poi.RestaurantName), _langService["OK"]);
            return;
        }

        string selectedLang = (_langService.CurrentLanguage ?? "vi").Trim().ToLower();
        var matched = poi.Narrations.FirstOrDefault(n =>
            n.Language != null && n.Language.Code?.Trim().ToLower() == selectedLang);

        if (matched == null)
        {
            await DisplayAlert(_langService["Notification"], _langService["NoNarrationLang"], _langService["OK"]);
            return;
        }

        // Log narration play for analytics
        _ = LogNarrationPlayAsync(poi, matched);

        btnPlay.IsVisible = false;
        btnStop.IsVisible = true;

        // If no audio file, fallback to TTS
        if (string.IsNullOrEmpty(matched.AudioUrl))
        {
            if (!string.IsNullOrEmpty(matched.TextContent))
            {
                _isSpeaking = true;
                _ttsCts?.Cancel();
                _ttsCts = new CancellationTokenSource();
                try
                {
                    await TextToSpeech.Default.SpeakAsync(matched.TextContent, cancelToken: _ttsCts.Token);
                }
                catch (OperationCanceledException) { }
                catch { }
                finally
                {
                    _isSpeaking = false;
                    btnPlay.IsVisible = true;
                    btnStop.IsVisible = false;
                }
            }
            return;
        }

        // Play audio file
        var fileName = Path.GetFileName(matched.AudioUrl);
        if (string.IsNullOrEmpty(fileName)) return;

        // Ưu tiên phát file offline nếu đã tải
        if (!string.IsNullOrEmpty(matched.LocalAudioPath) && File.Exists(matched.LocalAudioPath))
        {
            var localStream = File.OpenRead(matched.LocalAudioPath);
            var localPlayer = _audioManager.CreatePlayer(localStream);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _activePlayer = localPlayer;
                _activePlayer.PlaybackEnded += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() => StopAudio());
                };
                _activePlayer.Play();
            });
            return;
        }

        var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
        var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp;
        string audioUrl = $"http://{host}:5216/audios/{fileName}";

        try
        {
            _audioCts?.Cancel();
            _audioCts = new CancellationTokenSource();
            var token = _audioCts.Token;

            await Task.Run(async () =>
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                var response = await client.GetAsync(audioUrl, token);
                response.EnsureSuccessStatusCode();
                token.ThrowIfCancellationRequested();
                var stream = await response.Content.ReadAsStreamAsync();
                var player = _audioManager.CreatePlayer(stream);

                token.ThrowIfCancellationRequested();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        try { player.Dispose(); } catch { }
                        return;
                    }
                    _activePlayer = player;
                    _activePlayer.PlaybackEnded += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            StopAudio();
                        });
                    };
                    _activePlayer.Play();
                });
            }, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine("PlayPOINarration error: " + ex.Message);
            btnPlay.IsVisible = true;
            btnStop.IsVisible = false;
        }
    }

    private void StopAudio()
    {
        try
        {
            _audioCts?.Cancel();
            _ttsCts?.Cancel();
            _isSpeaking = false;

            var player = _activePlayer;
            _activePlayer = null;

            if (player != null)
            {
                try { player.Stop(); } catch { }
                try { player.Dispose(); } catch { }
            }
        }
        catch { }

        btnPlay.IsVisible = true;
        btnStop.IsVisible = false;
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if (_tour == null) return;
        var poi = _tour.POIs[_currentIndex];

        if (!IsValidCoordinates(poi.Latitude, poi.Longitude))
        {
            await DisplayAlert("Lỗi", "Điểm đến chưa có tọa độ hợp lệ", "OK");
            return;
        }

        try
        {
            var uri = new Uri($"google.navigation:q={poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            await Launcher.OpenAsync(uri);
        }
        catch
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(poi.Latitude, poi.Longitude, new MapLaunchOptions
            {
                Name = poi.RestaurantName,
                NavigationMode = NavigationMode.Driving
            });
        }
    }

    private async void OnBuyTourClicked(object sender, EventArgs e)
    {
        if (_tour == null) return;

        // Guest users cannot purchase tours
        var token = await SecureStorage.GetAsync("auth_token");
        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert(_langService["Error"],
                "Bạn cần đăng nhập để mua tour. Vui lòng đăng nhập trước.",
                _langService["OK"]);
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(TourPaymentPage)}?tourId={_tour.TourId}&tourName={Uri.EscapeDataString(_tour.Name)}&price={_tour.Price}");
    }

    private async Task CheckPaymentStatusAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return;

            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token)) return;

            var request = new HttpRequestMessage(HttpMethod.Get, $"api/payments/check-tour/{_tourId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var isPurchased = root.GetProperty("isPurchased").GetBoolean();
                var status = root.GetProperty("status").GetString() ?? "";

                if (isPurchased)
                {
                    _isPurchased = true;
                    paymentBanner.IsVisible = false;
                }
            }
        }
        catch { }
    }

    // GPS monitoring: auto-play narration when arriving at a POI
    private async void StartGpsMonitoring()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;

        double geofenceRadius = Preferences.Default.Get("geofence_radius", 5.0);

        while (_isMonitoring && _tour != null)
        {
            try
            {
                var loc = await _gpsService.GetCurrentLocation();
                if (loc != null)
                {
                    // Log GPS track point for analytics
                    _ = LogTrackPointAsync(loc.Latitude, loc.Longitude);

                    if (_activePlayer == null && !_isSpeaking && _isPurchased)
                    {
                        // Find the current POI the user is closest to
                        for (int i = 0; i < _tour.POIs.Count; i++)
                        {
                            var poi = _tour.POIs[i];
                            if (!IsValidCoordinates(poi.Latitude, poi.Longitude)) continue;
                            if (_autoPlayedPOIs.Contains(poi.RestaurantId)) continue;

                            var dist = _gpsService.CalculateDistance(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
                            if (dist <= geofenceRadius)
                            {
                                _autoPlayedPOIs.Add(poi.RestaurantId);
                                _currentIndex = i;

                                MainThread.BeginInvokeOnMainThread(() => ShowCurrentPOI());
                                await PlayPOINarration(poi);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.Delay(5000);
        }
    }

    private async Task LogNarrationPlayAsync(TourPOI poi, Narration narration)
    {
        try
        {
            var (userId, guestLabel) = await AnalyticsService.GetIdentityAsync();
            var response = await _httpClient.PostAsJsonAsync("api/analytics/narration-play", new
            {
                userId,
                restaurantId = poi.RestaurantId,
                tourId = _tourId,
                narrationId = narration.NarrationId,
                languageCode = narration.Language?.Code ?? _langService.CurrentLanguage ?? "vi",
                latitude = poi.Latitude,
                longitude = poi.Longitude,
                guestLabel
            });
            System.Diagnostics.Debug.WriteLine($"[TourDetail] LogNarrationPlay: status={response.StatusCode}, poi={poi.RestaurantId}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[TourDetail] LogNarrationPlay error: {body}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TourDetail] LogNarrationPlay exception: {ex.Message}");
        }
    }

    private async Task LogTrackPointAsync(double lat, double lng)
    {
        try
        {
            var (userId, guestLabel) = await AnalyticsService.GetIdentityAsync();
            var response = await _httpClient.PostAsJsonAsync("api/analytics/track-point", new
            {
                userId,
                tourId = _tourId,
                sessionId = _sessionId,
                latitude = lat,
                longitude = lng,
                guestLabel
            });
            System.Diagnostics.Debug.WriteLine($"[TourDetail] LogTrackPoint: status={response.StatusCode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TourDetail] LogTrackPoint exception: {ex.Message}");
        }
    }

    private static bool IsValidCoordinates(double lat, double lng)
    {
        return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 && (lat != 0 || lng != 0);
    }
}
