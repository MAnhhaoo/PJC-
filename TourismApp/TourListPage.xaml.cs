using System.Net.Http.Json;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

public partial class TourListPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly OfflineSyncService _offlineSyncService;
    private readonly LanguageService _lang;
    private readonly TranslationService _translationService;

    public TourListPage(HttpClient httpClient, OfflineSyncService offlineSyncService, LanguageService languageService, TranslationService translationService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _offlineSyncService = offlineSyncService;
        _lang = languageService;
        _translationService = translationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = _lang["TourList"];
        lblTourListTitle.Text = $"🗺️ {_lang["TourList"]}";
        lblChooseJourney.Text = _lang["ChooseJourney"];
        lblNoTours.Text = _lang["NoTours"];
        await LoadTours();
    }

    private async Task LoadTours()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            List<Tour> tours = null;

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    await _offlineSyncService.SyncToursAsync();
                    tours = await _offlineSyncService.GetToursOfflineAsync();
                }
                catch
                {
                    tours = await _offlineSyncService.GetToursOfflineAsync();
                }
            }
            else
            {
                tours = await _offlineSyncService.GetToursOfflineAsync();
            }

            if (tours != null)
            {
                // Translate tour data if language is not Vietnamese
                var currentLang = (_lang.CurrentLanguage ?? "vi").Trim().ToLower();
                if (currentLang != "vi")
                {
                    foreach (var t in tours)
                    {
                        t.Name = await _translationService.TranslateAsync(t.Name, currentLang);
                        t.Description = await _translationService.TranslateAsync(t.Description, currentLang);
                        foreach (var p in t.POIs)
                            p.RestaurantName = await _translationService.TranslateAsync(p.RestaurantName, currentLang);
                    }
                }

                var displayTours = tours.Select(t => new TourDisplay
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    POIRoutePreview = string.Join(" → ", t.POIs.OrderBy(p => p.OrderIndex).Select(p => p.RestaurantName)),
                    POICountText = $"{t.POIs.Count} {_lang["Destinations"]}",
                    PriceText = t.Price > 0 ? $"{t.Price:N0}đ" : _lang["Free"],
                    PriceBgColor = t.Price > 0 ? Color.FromArgb("#FFF3E0") : Color.FromArgb("#E8F5E9"),
                    PriceTextColor = t.Price > 0 ? Color.FromArgb("#E65100") : Color.FromArgb("#2E7D32")
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
        public string PriceText { get; set; } = "";
        public Color PriceBgColor { get; set; } = Color.FromArgb("#E8F5E9");
        public Color PriceTextColor { get; set; } = Color.FromArgb("#2E7D32");
    }
}
