using TourismApp.Services;
using Microsoft.Maui.Devices.Sensors;

namespace TourismApp;

[QueryProperty(nameof(RestaurantId), "restaurantId")]

public partial class RestaurantDetailPage : ContentPage
{
    private readonly DishService _dishService;
    private readonly RestaurantService _restaurantService;

    private int _restaurantId;

    private TourismApp.Models.Restaurant _restaurant;

    private Location _userLocation;

    public string RestaurantId
    {
        set => _restaurantId = int.Parse(value);
    }

    public RestaurantDetailPage(
        DishService dishService,
        RestaurantService restaurantService)
    {
        InitializeComponent();

        _dishService = dishService;
        _restaurantService = restaurantService;
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

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

    private async void OnOpenMap(object sender, EventArgs e)
    {
        if (_restaurant == null || _userLocation == null)
            return;

        try
        {
            var url =
                $"https://www.google.com/maps/dir/{_userLocation.Latitude},{_userLocation.Longitude}/{_restaurant.Latitude},{_restaurant.Longitude}";

            await Launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }
}