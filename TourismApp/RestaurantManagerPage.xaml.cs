using Plugin.Maui.Audio;
using System.Net.Http.Json;
using System.Text.Json;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

public partial class RestaurantManagerPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private readonly AuthService _authService; // Khai báo biến này để hết lỗi đỏ
    private bool _isBusy = false;

    // Constructor nhận 2 tham số để Dependency Injection hoạt động đúng
    public RestaurantManagerPage(HttpClient httpClient, AuthService authService , IAudioManager audioManager)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
        _audioManager = audioManager;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRestaurantStatus();
    }

    private async Task LoadRestaurantStatus()
    {
        try
        {
            _isBusy = true;
            var response = await _httpClient.GetAsync("api/restaurants/my");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var restaurant = JsonSerializer.Deserialize<JsonElement>(content);
                bool isActive = restaurant.GetProperty("isActive").GetBoolean();

                swStatus.IsToggled = isActive;
                lblStatusText.Text = isActive ? "Đang hoạt động" : "Đã đóng cửa";
            }
        }
        catch (Exception ex)
        {
            lblStatusText.Text = "Lỗi tải dữ liệu";
        }
        finally { _isBusy = false; }
    }

    private async void OnStatusToggled(object sender, ToggledEventArgs e)
    {
        if (_isBusy) return;
        try
        {
            bool newState = e.Value;
            var response = await _httpClient.PatchAsJsonAsync("api/restaurants/toggle-active", newState);
            if (response.IsSuccessStatusCode)
            {
                lblStatusText.Text = newState ? "Đang hoạt động" : "Đã đóng cửa";
            }
        }
        catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
    }

    // --- CÁC HÀM ĐIỀU HƯỚNG ---

    private async void GoDashboard(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("RestaurantDashboardPage");

    private async void GoAddDish(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("AddDishPage");

    private async void GoListDish(object sender, EventArgs e) =>
        await Navigation.PushAsync(new DishListPage(_httpClient));

    private async void GoEditRestaurant(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("EditRestaurantPage");

    // Điều hướng sang trang MyAudios
    private async void GoMyAudios(object sender, EventArgs e) =>
       await Navigation.PushAsync(new MyAudiosPage(_httpClient, _authService, _audioManager));

    // Điều hướng sang trang Nâng cấp
    private async void GoUpgradePremium(object sender, EventArgs e) =>
        await Navigation.PushAsync(new UpgradePremiumPage(_httpClient, _authService));
}