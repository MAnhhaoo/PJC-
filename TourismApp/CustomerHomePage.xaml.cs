using System.Collections.ObjectModel;
using TourismApp.Models;
using TourismApp.Services;
using Microsoft.Maui.Devices.Sensors;

namespace TourismApp;

public partial class CustomerHomePage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly UserService _userService;

    public ObservableCollection<Restaurant> Restaurants { get; set; } = new();

    public CustomerHomePage(
        RestaurantService restaurantService,
        UserService userService)
    {
        InitializeComponent();

        _restaurantService = restaurantService;
        _userService = userService;

        RestaurantList.ItemsSource = Restaurants;
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var user = await _userService.GetMeAsync();
            var allRestaurants = await _restaurantService.GetRestaurantsAsync();

            // Lọc restaurant
            List<Restaurant> filteredRestaurants;

            if (user.UserLevel == 0) // FREE USER
            {
                filteredRestaurants = allRestaurants
                    .Where(r => !r.IsPremium && r.IsApproved)
                    .ToList();
            }
            else // VIP USER
            {
                filteredRestaurants = allRestaurants
                    .Where(r => r.IsApproved)
                    .ToList();
            }

            // Lấy vị trí user
            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.GetLocationAsync(request);

            // Tính khoảng cách
            if (location != null)
            {
                foreach (var r in filteredRestaurants)
                {
                    r.Distance = Location.CalculateDistance(
                        location.Latitude,
                        location.Longitude,
                        r.Latitude,
                        r.Longitude,
                        DistanceUnits.Kilometers);
                }

                // sort gần nhất
                filteredRestaurants = filteredRestaurants
                    .OrderBy(r => r.Distance)
                    .ToList();
            }

            Restaurants.Clear();

            foreach (var item in filteredRestaurants)
                Restaurants.Add(item);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnOpenMap(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            var restaurant = button?.BindingContext as Restaurant;

            if (restaurant == null)
                return;

            await Navigation.PushAsync(new RestaurantMapPage(restaurant));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
    // CLICK restaurant
    private async void OnRestaurantTapped(object sender, TappedEventArgs e)
    {
        int restaurantId = (int)e.Parameter;

        await Shell.Current.GoToAsync($"RestaurantDetailPage?restaurantId={restaurantId}");
    }
}