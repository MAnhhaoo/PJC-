using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Plugin.Maui.Audio;
using TourismApp.Services;
using TourismApp.Models;

namespace TourismApp;

public partial class POIMapPage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly GpsService _gpsService;
    private readonly IAudioManager _audioManager;
    private readonly AnalyticsService _analyticsService;
    private readonly OfflineSyncService _offlineSyncService;
    private readonly LanguageService _langService;
    private readonly TranslationService _translationService;
    private CancellationTokenSource _cts;
    private Location _userLocation;
    private IAudioPlayer _currentPlayer;
    private Restaurant _selectedRestaurant;
    private bool _initialLocationSet;

    public ObservableCollection<Restaurant> Restaurants { get; set; } = new();

    public POIMapPage(RestaurantService restaurantService, GpsService gpsService, IAudioManager audioManager, AnalyticsService analyticsService, OfflineSyncService offlineSyncService, LanguageService languageService, TranslationService translationService)
    {
        InitializeComponent();
        _restaurantService = restaurantService;
        _gpsService = gpsService;
        _audioManager = audioManager;
        _analyticsService = analyticsService;
        _offlineSyncService = offlineSyncService;
        _langService = languageService;
        _translationService = translationService;
        this.BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _initialLocationSet = false;
        await LoadRestaurants();
        StartLocationUpdates();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLocationUpdates();
        StopAudio();
    }

    private async Task LoadRestaurants()
    {
        try
        {
            List<Restaurant> list;
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await _offlineSyncService.SyncRestaurantsAsync();
                await _offlineSyncService.DownloadAudioFilesAsync();
                list = await _offlineSyncService.GetRestaurantsOfflineAsync();
            }
            else
            {
                list = await _offlineSyncService.GetRestaurantsOfflineAsync();
            }
            // Translate restaurant data if language is not Vietnamese
            var lang = (_langService.CurrentLanguage ?? "vi").Trim().ToLower();
            if (lang != "vi")
                await _translationService.TranslateRestaurantsAsync(list, lang);

            Restaurants.Clear();
            foreach (var r in list) Restaurants.Add(r);
            RenderPins();
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            await DisplayAlert("Lỗi", "Không thể tải POI: " + msg, "OK");
        }
    }

    private void RenderPins()
    {
        map.Pins.Clear();
        map.MapElements.Clear();

        foreach (var r in Restaurants)
        {
            var pin = new Pin
            {
                Label = r.Name,
                Address = r.Address,
                Location = new Location(r.Latitude, r.Longitude),
                Type = PinType.Place
            };
            pin.MarkerClicked += OnPinClicked;
            map.Pins.Add(pin);
        }
    }

    private async void OnPinClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;
        if (sender is not Pin pin) return;

        var restaurant = Restaurants.FirstOrDefault(r =>
            Math.Abs(r.Latitude - pin.Location.Latitude) < 0.0001 &&
            Math.Abs(r.Longitude - pin.Location.Longitude) < 0.0001);

        if (restaurant == null) return;

        _selectedRestaurant = restaurant;

        // Auto-play audio
        await PlayRestaurantAudio(restaurant);

        // Draw route from user location to this restaurant
        if (_userLocation != null)
        {
            await DrawRouteToRestaurant(restaurant);
        }
        else
        {
            // No user location yet, just show name
            lblRouteName.Text = restaurant.Name;
            lblRouteInfo.Text = "Đang lấy vị trí...";
            routeInfoCard.IsVisible = true;
        }
    }

    private async Task DrawRouteToRestaurant(Restaurant restaurant)
    {
        try
        {
            var dest = new Location(restaurant.Latitude, restaurant.Longitude);

            // Fetch route + info on background thread
            var result = await Task.Run(() => GetRouteWithInfo(_userLocation, dest));

            if (result.Points == null || result.Points.Count == 0) return;

            // Draw polyline
            map.MapElements.Clear();
            var polyline = new Polyline { StrokeColor = Colors.Blue, StrokeWidth = 6 };
            foreach (var point in result.Points)
                polyline.Geopath.Add(point);
            map.MapElements.Add(polyline);

            // Center map
            var centerLat = (_userLocation.Latitude + restaurant.Latitude) / 2.0;
            var centerLng = (_userLocation.Longitude + restaurant.Longitude) / 2.0;
            var dist = _gpsService.CalculateDistance(_userLocation.Latitude, _userLocation.Longitude, restaurant.Latitude, restaurant.Longitude);
            var km = Math.Max(dist / 1000.0 * 0.8, 0.5);
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(centerLat, centerLng), Distance.FromKilometers(km)));

            // Show info card
            lblRouteName.Text = restaurant.Name;
            var distText = result.DistanceKm >= 1 ? $"{result.DistanceKm:F1} km" : $"{result.DistanceKm * 1000:F0} m";
            var mins = (int)Math.Ceiling(result.DurationMin);
            lblRouteInfo.Text = $"📍 {distText}  •  🕐 ~{mins} phút lái xe";
            routeInfoCard.IsVisible = true;
        }
        catch { }
    }

    private async Task<(List<Location> Points, double DistanceKm, double DurationMin)> GetRouteWithInfo(Location start, Location end)
    {
        var apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjAxNjY2NDQxYjU3OTQ1N2E5Y2I1NjgxZTkxOGMwZTg3IiwiaCI6Im11cm11cjY0In0=";
        var url = $"https://api.openrouteservice.org/v2/directions/driving-car?start={start.Longitude.ToString(CultureInfo.InvariantCulture)},{start.Latitude.ToString(CultureInfo.InvariantCulture)}&end={end.Longitude.ToString(CultureInfo.InvariantCulture)},{end.Latitude.ToString(CultureInfo.InvariantCulture)}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);

        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return ([], 0, 0);

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(json);

        if (!data.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            return ([], 0, 0);

        var feature = features[0];
        var coordinates = feature.GetProperty("geometry").GetProperty("coordinates");
        var route = new List<Location>();

        foreach (var point in coordinates.EnumerateArray())
        {
            double lng = point[0].GetDouble();
            double lat = point[1].GetDouble();
            route.Add(new Location(lat, lng));
        }

        // Extract distance (meters) and duration (seconds) from summary
        double distKm = 0, durMin = 0;
        if (feature.TryGetProperty("properties", out var props) &&
            props.TryGetProperty("summary", out var summary))
        {
            if (summary.TryGetProperty("distance", out var d)) distKm = d.GetDouble() / 1000.0;
            if (summary.TryGetProperty("duration", out var t)) durMin = t.GetDouble() / 60.0;
        }

        return (route, distKm, durMin);
    }

    private void StartLocationUpdates()
    {
        StopLocationUpdates();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var loc = await _gpsService.GetCurrentLocation();
                        if (loc != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _userLocation = loc;
                                UpdateDistances(loc);

                                if (!_initialLocationSet)
                                {
                                    _initialLocationSet = true;
                                    map.MoveToRegion(MapSpan.FromCenterAndRadius(
                                        new Location(loc.Latitude, loc.Longitude),
                                        Distance.FromKilometers(2)));
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch { }
                    await Task.Delay(5000, token);
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void StopLocationUpdates()
    {
        try { _cts?.Cancel(); } catch { }
        _cts = null;
    }

    private void UpdateDistances(Location userLoc)
    {
        if (userLoc == null) return;

        foreach (var r in Restaurants)
        {
            r.Distance = _gpsService.CalculateDistance(userLoc.Latitude, userLoc.Longitude, r.Latitude, r.Longitude);
            r.IsNearest = false;
        }

        var nearest = Restaurants.OrderBy(r => r.Distance).FirstOrDefault();
        if (nearest != null)
        {
            nearest.IsNearest = true;
        }

        // Re-render pins (only restaurant pins, no user pin)
        map.Pins.Clear();
        foreach (var r in Restaurants)
        {
            var pin = new Pin
            {
                Label = r.IsNearest ? $"⭐ {r.Name} (Gần nhất)" : r.Name,
                Address = r.Address,
                Location = new Location(r.Latitude, r.Longitude),
                Type = PinType.Place
            };
            pin.MarkerClicked += OnPinClicked;
            map.Pins.Add(pin);
        }
    }

    private void StopAudio()
    {
        try { _currentPlayer?.Stop(); _currentPlayer?.Dispose(); } catch { }
        _currentPlayer = null;
    }

    private async Task PlayRestaurantAudio(Restaurant restaurant)
    {
        try
        {
            StopAudio();

            if (restaurant.Narrations == null || !restaurant.Narrations.Any()) return;
            var lang = (_langService.CurrentLanguage ?? "vi").Trim().ToLower();
            var matched = restaurant.Narrations.FirstOrDefault(n => n.Language != null && n.Language.Code?.Trim().ToLower() == lang);
            if (matched == null)
            {
                await DisplayAlert(_langService["Notification"], _langService["NoNarrationLang"], _langService["OK"]);
                return;
            }

            _ = _analyticsService.LogNarrationPlayAsync(
                restaurant.RestaurantId, null, matched.NarrationId,
                matched.Language?.Code ?? lang, restaurant.Latitude, restaurant.Longitude);

            // Ưu tiên phát file offline nếu đã tải
            if (!string.IsNullOrEmpty(matched.LocalAudioPath) && File.Exists(matched.LocalAudioPath))
            {
                var stream = File.OpenRead(matched.LocalAudioPath);
                _currentPlayer = _audioManager.CreatePlayer(stream);
                _currentPlayer.Play();
                return;
            }

            var fileName = string.IsNullOrEmpty(matched.AudioUrl) ? string.Empty : Path.GetFileName(matched.AudioUrl);
            if (string.IsNullOrEmpty(fileName))
            {
                if (!string.IsNullOrEmpty(matched.TextContent))
                {
                    await TextToSpeech.Default.SpeakAsync(matched.TextContent);
                }
                return;
            }

            var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp;
            var audioUrl = new Uri(new Uri($"http://{host}:5216/"), $"audios/{fileName}");

            // Download audio on background thread to avoid NetworkOnMainThreadException
            try
            {
                var audioData = await Task.Run(async () =>
                {
                    using var http = new HttpClient();
                    return await http.GetByteArrayAsync(audioUrl);
                });

                var stream2 = new MemoryStream(audioData);
                _currentPlayer = _audioManager.CreatePlayer(stream2);
                _currentPlayer.Play();
            }
            catch
            {
                // Audio file not found on server — fallback to TextToSpeech
                if (!string.IsNullOrEmpty(matched.TextContent))
                {
                    await TextToSpeech.Default.SpeakAsync(matched.TextContent);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi phát", ex.Message, "OK");
        }
    }

    private async void OnOverlayNavigateClicked(object sender, EventArgs e)
    {
        if (_selectedRestaurant == null) return;
        var destLat = _selectedRestaurant.Latitude.ToString(CultureInfo.InvariantCulture);
        var destLng = _selectedRestaurant.Longitude.ToString(CultureInfo.InvariantCulture);

        string url;
        if (DeviceInfo.Platform == DevicePlatform.Android)
            url = $"google.navigation:q={destLat},{destLng}&mode=d";
        else if (_userLocation != null)
            url = $"https://www.google.com/maps/dir/{_userLocation.Latitude.ToString(CultureInfo.InvariantCulture)},{_userLocation.Longitude.ToString(CultureInfo.InvariantCulture)}/{destLat},{destLng}";
        else
            url = $"https://www.google.com/maps/search/?api=1&query={destLat},{destLng}";

        await Launcher.OpenAsync(url);
    }

    private async void OnNavigateClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is Restaurant r)
        {
            var destLat = r.Latitude.ToString(CultureInfo.InvariantCulture);
            var destLng = r.Longitude.ToString(CultureInfo.InvariantCulture);

            string url;
            if (DeviceInfo.Platform == DevicePlatform.Android)
                url = $"google.navigation:q={destLat},{destLng}&mode=d";
            else if (_userLocation != null)
                url = $"https://www.google.com/maps/dir/{_userLocation.Latitude.ToString(CultureInfo.InvariantCulture)},{_userLocation.Longitude.ToString(CultureInfo.InvariantCulture)}/{destLat},{destLng}";
            else
                url = $"https://www.google.com/maps/search/?api=1&query={destLat},{destLng}";

            await Launcher.OpenAsync(url);
        }
    }

    private async Task OpenInChrome(string url)
    {
        try
        {
#if ANDROID
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView, Android.Net.Uri.Parse(url));
            intent.SetPackage("com.android.chrome");
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            Platform.CurrentActivity?.StartActivity(intent);
#else
            await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
#endif
        }
        catch
        {
            try
            {
                await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể mở trình duyệt: " + ex.Message, "OK");
            }
        }
    }

    private void OnCloseRouteClicked(object sender, EventArgs e)
    {
        routeInfoCard.IsVisible = false;
        map.MapElements.Clear();
        _selectedRestaurant = null;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadRestaurants();
    }
}
