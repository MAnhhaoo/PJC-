using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
namespace TourismApp;

public partial class MapPage : ContentPage
{
    string apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjAxNjY2NDQxYjU3OTQ1N2E5Y2I1NjgxZTkxOGMwZTg3IiwiaCI6Im11cm11cjY0In0=";

    public MapPage()
    {
        InitializeComponent();
        LoadRoute();
    }

    async void LoadRoute()
    {
        double startLat = 10.776889;
        double startLng = 106.700806;

        double endLat = 10.762622;
        double endLng = 106.660172;

        string url =
        $"https://api.openrouteservice.org/v2/directions/driving-car?start={startLng},{startLat}&end={endLng},{endLat}";

        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", apiKey);

        var response = await client.GetStringAsync(url);

        var json = JsonDocument.Parse(response);

        var coordinates = json
            .RootElement
            .GetProperty("features")[0]
            .GetProperty("geometry")
            .GetProperty("coordinates");

        Polyline route = new Polyline
        {
            StrokeColor = Colors.Blue,
            StrokeWidth = 6
        };

        foreach (var point in coordinates.EnumerateArray())
        {
            double lng = point[0].GetDouble();
            double lat = point[1].GetDouble();

            route.Geopath.Add(new Location(lat, lng));
        }

        map.MapElements.Add(route);

        map.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                new Location(startLat, startLng),
                Distance.FromKilometers(5)
            )
        );
    }
}