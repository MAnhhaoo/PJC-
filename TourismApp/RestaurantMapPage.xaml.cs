using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;
using TourismApp.Models;

namespace TourismApp;

public partial class RestaurantMapPage : ContentPage
{
    private readonly Restaurant _restaurant;
    private Location _userLocation;

    public RestaurantMapPage(Restaurant restaurant)
    {
        InitializeComponent();
        _restaurant = restaurant;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
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

            // PIN USER
            RestaurantMap.Pins.Add(new Pin
            {
                Label = "Bạn đang ở đây",
                Location = _userLocation,
                Type = PinType.Generic
            });

            // PIN RESTAURANT
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
            $"https://api.openrouteservice.org/v2/directions/driving-car?start={start.Longitude},{start.Latitude}&end={end.Longitude},{end.Latitude}";

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
        var url =
            $"https://www.google.com/maps/dir/?api=1&destination={_restaurant.Latitude},{_restaurant.Longitude}&travelmode=driving";

        await Launcher.OpenAsync(url);
    }
}