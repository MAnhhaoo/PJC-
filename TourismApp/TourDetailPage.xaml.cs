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

    private Tour? _tour;
    private int _currentIndex = 0;
    private IAudioPlayer? _activePlayer;
    private bool _isSpeaking;
    private CancellationTokenSource? _ttsCts;
    private CancellationTokenSource? _audioCts;
    private bool _isMonitoring;
    private readonly HashSet<int> _autoPlayedPOIs = new();
    private readonly string _sessionId = Guid.NewGuid().ToString();

    public string TourIdStr
    {
        set
        {
            if (int.TryParse(value, out var id))
                _tourId = id;
        }
    }
    private int _tourId;

    public TourDetailPage(HttpClient httpClient, IAudioManager audioManager, GpsService gpsService, LanguageService languageService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _audioManager = audioManager;
        _gpsService = gpsService;
        _langService = languageService;
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
            _tour = await _httpClient.GetFromJsonAsync<Tour>($"api/tours/{_tourId}");
            if (_tour == null || !_tour.POIs.Any())
            {
                await DisplayAlert("Lỗi", "Không tìm thấy tour hoặc tour chưa có điểm đến", "OK");
                return;
            }

            _tour.POIs = _tour.POIs.OrderBy(p => p.OrderIndex).ToList();

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

            lblTourName.Text = _tour.Name;
            lblTourDesc.Text = _tour.Description;
            lblPOICount.Text = $"Tổng: {_tour.POIs.Count} điểm";

            _currentIndex = 0;
            ShowCurrentPOI();
            DrawRoute();
            StartGpsMonitoring();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tải tour: " + ex.Message, "OK");
        }
    }

    private void ShowCurrentPOI()
    {
        if (_tour == null || !_tour.POIs.Any()) return;

        var poi = _tour.POIs[_currentIndex];

        lblCurrentPOI.Text = $"▶ Điểm {_currentIndex + 1}/{_tour.POIs.Count}";
        lblPOIOrder.Text = $"ĐIỂM {poi.OrderIndex}";
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
            await DisplayAlert("Thông báo", $"Chưa có thuyết minh cho {poi.RestaurantName}", "OK");
            return;
        }

        string selectedLang = (_langService.CurrentLanguage ?? "vi").Trim().ToLower();
        var matched = poi.Narrations.FirstOrDefault(n =>
            n.Language != null && n.Language.Code?.Trim().ToLower() == selectedLang)
            ?? poi.Narrations.FirstOrDefault();

        if (matched == null) return;

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

                    if (_activePlayer == null && !_isSpeaking)
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
            var userId = await GetUserIdFromToken();
            await _httpClient.PostAsJsonAsync("api/analytics/narration-play", new
            {
                userId,
                restaurantId = poi.RestaurantId,
                tourId = _tourId,
                narrationId = narration.NarrationId,
                languageCode = narration.Language?.Code ?? _langService.CurrentLanguage ?? "vi",
                latitude = poi.Latitude,
                longitude = poi.Longitude
            });
        }
        catch { }
    }

    private async Task LogTrackPointAsync(double lat, double lng)
    {
        try
        {
            var userId = await GetUserIdFromToken();
            await _httpClient.PostAsJsonAsync("api/analytics/track-point", new
            {
                userId,
                tourId = _tourId,
                sessionId = _sessionId,
                latitude = lat,
                longitude = lng
            });
        }
        catch { }
    }

    private static async Task<int> GetUserIdFromToken()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token)) return 0;

            var parts = token.Split('.');
            if (parts.Length < 2) return 0;

            var payload = parts[1];
            // Fix base64 padding
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try common claim names for user id
            foreach (var claimName in new[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "sub", "nameid" })
            {
                if (root.TryGetProperty(claimName, out var val))
                {
                    var s = val.ToString();
                    if (int.TryParse(s, out var id)) return id;
                }
            }
            return 0;
        }
        catch { return 0; }
    }

    private static bool IsValidCoordinates(double lat, double lng)
    {
        return lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 && (lat != 0 || lng != 0);
    }
}
