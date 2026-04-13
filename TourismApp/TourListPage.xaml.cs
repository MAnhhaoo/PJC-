using System.Net.Http.Json;
using TourismApp.Models;

namespace TourismApp;

public partial class TourListPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public TourListPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadTours();
    }

    private async Task LoadTours()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var tours = await _httpClient.GetFromJsonAsync<List<Tour>>("api/tours");
            if (tours != null)
            {
                var displayTours = tours.Select(t => new TourDisplay
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    POIRoutePreview = string.Join(" → ", t.POIs.OrderBy(p => p.OrderIndex).Select(p => p.RestaurantName)),
                    POICountText = $"{t.POIs.Count} điểm đến"
                }).ToList();

                TourCollection.ItemsSource = displayTours;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadTours error: " + ex.Message);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnTourTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int tourId)
        {
            await Shell.Current.GoToAsync($"{nameof(TourDetailPage)}?tourId={tourId}");
        }
    }

    // Display model for binding
    public class TourDisplay
    {
        public int TourId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string POIRoutePreview { get; set; } = "";
        public string POICountText { get; set; } = "";
    }
}
