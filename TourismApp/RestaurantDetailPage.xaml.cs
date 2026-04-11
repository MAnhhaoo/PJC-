using TourismApp.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Plugin.Maui.Audio;
using System.Globalization;
using System.Text.Json;

namespace TourismApp;

[QueryProperty(nameof(RestaurantId), "restaurantId")]

public partial class RestaurantDetailPage : ContentPage
{
    private readonly DishService _dishService;
    private readonly RestaurantService _restaurantService;
    private readonly IAudioManager _audioManager;

    private int _restaurantId;

    private TourismApp.Models.Restaurant _restaurant;

    private Location _userLocation;
    private IAudioPlayer _currentPlayer;
    private bool _isPlayingAudio;

    public string RestaurantId
    {
        set => _restaurantId = int.Parse(value);
    }

    public RestaurantDetailPage(
        DishService dishService,
        RestaurantService restaurantService,
        IAudioManager audioManager)
    {
        InitializeComponent();

        _dishService = dishService;
        _restaurantService = restaurantService;
        _audioManager = audioManager;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _restaurant = await _restaurantService.GetRestaurantByIdAsync(_restaurantId);

            if (_restaurant == null)
                return;

            RestaurantName.Text = _restaurant.Name;
            RestaurantAddress.Text = _restaurant.Address;
            RestaurantDescription.Text = _restaurant.Description;
            RestaurantImage.Source = _restaurant.Image;

            var dishes = await _dishService.GetDishesByRestaurantAsync(_restaurantId);
            DishList.ItemsSource = dishes;

            await LoadUserLocationAndDistance();
            await LoadMapWithRoute();
            SetupAudio();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAudio();
    }

    private static bool IsValidCoordinates(double lat, double lng)
        => lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180 && (lat != 0 || lng != 0);

    private async Task LoadUserLocationAndDistance()
    {
        try
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            _userLocation = await Geolocation.GetLocationAsync(request);

            if (_userLocation == null)
                return;

            if (!IsValidCoordinates(_restaurant.Latitude, _restaurant.Longitude))
            {
                DistanceLabel.Text = "📍 Nhà hàng chưa cập nhật tọa độ";
                return;
            }

            double distance = Location.CalculateDistance(
                _userLocation.Latitude,
                _userLocation.Longitude,
                _restaurant.Latitude,
                _restaurant.Longitude,
                DistanceUnits.Kilometers);

            DistanceLabel.Text = $"📍 {Math.Round(distance, 2)} km từ vị trí của bạn";
        }
        catch
        {
            DistanceLabel.Text = "";
        }
    }

    private async Task LoadMapWithRoute()
    {
        if (_restaurant == null) return;
        if (!IsValidCoordinates(_restaurant.Latitude, _restaurant.Longitude))
        {
            MapSection.IsVisible = false;
            NoLocationFrame.IsVisible = true;
            return;
        }

        MapSection.IsVisible = true;
        NoLocationFrame.IsVisible = false;

        var restaurantLocation = new Location(_restaurant.Latitude, _restaurant.Longitude);

        DetailMap.Pins.Clear();
        DetailMap.MapElements.Clear();

        DetailMap.Pins.Add(new Pin
        {
            Label = _restaurant.Name,
            Address = _restaurant.Address,
            Location = restaurantLocation,
            Type = PinType.Place
        });

        if (_userLocation != null)
        {
            DetailMap.Pins.Add(new Pin
            {
                Label = "Bạn đang ở đây",
                Location = _userLocation,
                Type = PinType.Generic
            });

            var route = await GetRoute(_userLocation, restaurantLocation);

            if (route != null && route.Count > 0)
            {
                var polyline = new Polyline
                {
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 6
                };
                foreach (var point in route)
                    polyline.Geopath.Add(point);
                DetailMap.MapElements.Add(polyline);

                var centerLat = (_userLocation.Latitude + _restaurant.Latitude) / 2.0;
                var centerLng = (_userLocation.Longitude + _restaurant.Longitude) / 2.0;
                DetailMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(centerLat, centerLng),
                    Distance.FromKilometers(2)));
            }
            else
            {
                DetailMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    restaurantLocation, Distance.FromKilometers(1)));
            }
        }
        else
        {
            DetailMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                restaurantLocation, Distance.FromKilometers(1)));
        }
    }

    private async Task<List<Location>> GetRoute(Location start, Location end)
    {
        var apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjAxNjY2NDQxYjU3OTQ1N2E5Y2I1NjgxZTkxOGMwZTg3IiwiaCI6Im11cm11cjY0In0=";
        var url = $"https://api.openrouteservice.org/v2/directions/driving-car?start={start.Longitude.ToString(CultureInfo.InvariantCulture)},{start.Latitude.ToString(CultureInfo.InvariantCulture)}&end={end.Longitude.ToString(CultureInfo.InvariantCulture)},{end.Latitude.ToString(CultureInfo.InvariantCulture)}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);

        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(json);

        if (!data.RootElement.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
            return [];

        var coordinates = features[0].GetProperty("geometry").GetProperty("coordinates");
        var route = new List<Location>();

        foreach (var point in coordinates.EnumerateArray())
        {
            double lng = point[0].GetDouble();
            double lat = point[1].GetDouble();
            route.Add(new Location(lat, lng));
        }

        return route;
    }

    private async void SetupAudio()
    {
        if (_restaurant?.Narrations != null && _restaurant.Narrations.Count > 0)
        {
            AudioFrame.IsVisible = true;
            await PlayRestaurantAudio();
        }
    }

    private async void OnToggleAudio(object sender, EventArgs e)
    {
        if (_isPlayingAudio)
        {
            StopAudio();
            AudioButton.Text = "▶ Phát thuyết minh";
            AudioStatusLabel.Text = "";
            _isPlayingAudio = false;
        }
        else
        {
            await PlayRestaurantAudio();
        }
    }

    private async Task PlayRestaurantAudio()
    {
        try
        {
            StopAudio();

            if (_restaurant?.Narrations == null || _restaurant.Narrations.Count == 0) return;

            var lang = "vi";
            var matched = _restaurant.Narrations.FirstOrDefault(n => n.Language != null && n.Language.Code == lang)
                          ?? _restaurant.Narrations.FirstOrDefault();
            if (matched == null) return;

            var fileName = string.IsNullOrEmpty(matched.AudioUrl) ? string.Empty : Path.GetFileName(matched.AudioUrl);

            if (string.IsNullOrEmpty(fileName))
            {
                if (!string.IsNullOrEmpty(matched.TextContent))
                {
                    _isPlayingAudio = true;
                    AudioButton.Text = "⏹ Dừng thuyết minh";
                    AudioStatusLabel.Text = "Đang đọc...";
                    await TextToSpeech.Default.SpeakAsync(matched.TextContent);
                    _isPlayingAudio = false;
                    AudioButton.Text = "▶ Phát thuyết minh";
                    AudioStatusLabel.Text = "";
                }
                return;
            }

            var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp;
            var audioUrl = new Uri(new Uri($"http://{host}:5216/"), $"audios/{fileName}");

            AudioButton.Text = "⏳ Đang tải...";
            AudioStatusLabel.Text = "Đang tải audio...";

            var audioData = await Task.Run(async () =>
            {
                using var http = new HttpClient();
                return await http.GetByteArrayAsync(audioUrl);
            });

            var stream = new MemoryStream(audioData);
            _currentPlayer = _audioManager.CreatePlayer(stream);
            _currentPlayer.PlaybackEnded += OnPlaybackEnded;
            _currentPlayer.Play();

            _isPlayingAudio = true;
            AudioButton.Text = "⏹ Dừng thuyết minh";
            AudioStatusLabel.Text = "Đang phát thuyết minh...";
        }
        catch (Exception ex)
        {
            AudioStatusLabel.Text = "Lỗi phát audio";
            await DisplayAlert("Lỗi phát", ex.Message, "OK");
        }
    }

    private void OnPlaybackEnded(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isPlayingAudio = false;
            AudioButton.Text = "▶ Phát thuyết minh";
            AudioStatusLabel.Text = "";
        });
    }

    private void StopAudio()
    {
        try
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.PlaybackEnded -= OnPlaybackEnded;
                _currentPlayer.Stop();
                _currentPlayer.Dispose();
            }
        }
        catch { }
        _currentPlayer = null;
    }

    private async void OnOpenMap(object sender, EventArgs e)
    {
        if (_restaurant == null || _userLocation == null)
            return;

        if (!IsValidCoordinates(_restaurant.Latitude, _restaurant.Longitude))
        {
            await DisplayAlert("Lỗi", "Nhà hàng chưa có tọa độ. Chủ nhà hàng cần cập nhật vị trí GPS.", "OK");
            return;
        }

        try
        {
            var destLat = _restaurant.Latitude.ToString(CultureInfo.InvariantCulture);
            var destLng = _restaurant.Longitude.ToString(CultureInfo.InvariantCulture);

            string url;
            if (DeviceInfo.Platform == DevicePlatform.Android)
                url = $"google.navigation:q={destLat},{destLng}&mode=d";
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
                url = $"http://maps.apple.com/?daddr={destLat},{destLng}&dirflg=d";
            else
                url = $"https://www.google.com/maps/dir/?api=1&origin={_userLocation.Latitude.ToString(CultureInfo.InvariantCulture)},{_userLocation.Longitude.ToString(CultureInfo.InvariantCulture)}&destination={destLat},{destLng}";

            await Launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }
}