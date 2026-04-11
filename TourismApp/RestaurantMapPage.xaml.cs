using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Text.Json;

namespace TourismApp;

public partial class RestaurantMapPage : ContentPage
{
    private readonly TourismApp.Models.Restaurant _restaurant;
    private Location _userLocation;

    public RestaurantMapPage(TourismApp.Models.Restaurant restaurant)
    {
        InitializeComponent();
        _restaurant = restaurant;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Debug: kiểm tra tọa độ nhà hàng
            if (_restaurant.Latitude == 0 && _restaurant.Longitude == 0)
            {
                await DisplayAlert("Lỗi tọa độ",
                    $"Nhà hàng '{_restaurant.Name}' chưa có tọa độ (0, 0).\nVui lòng vào Chỉnh sửa nhà hàng → Lấy vị trí GPS → Lưu lại.",
                    "OK");
                return;
            }

            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.GetLocationAsync(request);

            if (location == null)
            {
                await DisplayAlert("Lỗi", "Không lấy được vị trí", "OK");
                return;
            }

            _userLocation = new Location(location.Latitude, location.Longitude);

            var restaurantLocation =
                new Location(_restaurant.Latitude, _restaurant.Longitude);

            RestaurantMap.Pins.Clear();
            RestaurantMap.MapElements.Clear();

            // USER
            RestaurantMap.Pins.Add(new Pin
            {
                Label = "Bạn đang ở đây",
                Location = _userLocation,
                Type = PinType.Generic
            });

            // RESTAURANT
            RestaurantMap.Pins.Add(new Pin
            {
                Label = _restaurant.Name,
                Address = _restaurant.Address,
                Location = restaurantLocation,
                Type = PinType.Place
            });

            var route = await GetRoute(_userLocation, restaurantLocation);

            if (route == null || route.Count == 0)
            {
                await DisplayAlert("Lỗi", "Không lấy được đường đi", "OK");
                return;
            }

            var polyline = new Polyline
            {
                StrokeColor = Colors.Blue,
                StrokeWidth = 6
            };

            foreach (var point in route)
                polyline.Geopath.Add(point);

            RestaurantMap.MapElements.Add(polyline);

            RestaurantMap.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    route[route.Count / 2],
                    Distance.FromKilometers(2)
                ));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    async Task<List<Location>> GetRoute(Location start, Location end)
    {
        var apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjAxNjY2NDQxYjU3OTQ1N2E5Y2I1NjgxZTkxOGMwZTg3IiwiaCI6Im11cm11cjY0In0=";

        var url =
            $"https://api.openrouteservice.org/v2/directions/driving-car?start={start.Longitude.ToString(CultureInfo.InvariantCulture)},{start.Latitude.ToString(CultureInfo.InvariantCulture)}&end={end.Longitude.ToString(CultureInfo.InvariantCulture)},{end.Latitude.ToString(CultureInfo.InvariantCulture)}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);

        var response = await http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<Location>();

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(json);

        if (!data.RootElement.TryGetProperty("features", out var features))
            return new List<Location>();

        if (features.GetArrayLength() == 0)
            return new List<Location>();

        var coordinates = features[0]
            .GetProperty("geometry")
            .GetProperty("coordinates");

        var route = new List<Location>();

        foreach (var point in coordinates.EnumerateArray())
        {
            double lng = point[0].GetDouble();
            double lat = point[1].GetDouble();

            route.Add(new Location(lat, lng));
        }

        return route;
    }

    private async void OpenGoogleMaps(object sender, EventArgs e)
    {
        if (_restaurant.Latitude == 0 && _restaurant.Longitude == 0)
        {
            await DisplayAlert("Lỗi", "Nhà hàng chưa có tọa độ. Vui lòng cập nhật vị trí GPS trong phần Chỉnh sửa nhà hàng.", "OK");
            return;
        }

        var lat = _restaurant.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = _restaurant.Longitude.ToString(CultureInfo.InvariantCulture);

        string url;
        if (DeviceInfo.Platform == DevicePlatform.Android)
            url = $"google.navigation:q={lat},{lng}&mode=d";
        else if (DeviceInfo.Platform == DevicePlatform.iOS)
            url = $"http://maps.apple.com/?daddr={lat},{lng}&dirflg=d";
        else
            url = $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}&travelmode=driving";

        await Launcher.OpenAsync(url);
    }
}