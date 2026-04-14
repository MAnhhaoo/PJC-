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
    private readonly AuthService _authService;
    private bool _isBusy = false;
    private string _selectedPlan = "Normal"; // Default plan

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
            await _authService.SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("api/restaurants/my");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // User has no restaurant yet
                noRestaurantSection.IsVisible = true;
                managementSection.IsVisible = false;
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var restaurant = JsonSerializer.Deserialize<JsonElement>(content);
                bool isActive = restaurant.GetProperty("isActive").GetBoolean();

                swStatus.IsToggled = isActive;
                lblStatusText.Text = isActive ? "Đang hoạt động" : "Đã đóng cửa";

                noRestaurantSection.IsVisible = false;
                managementSection.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            lblStatusText.Text = "Lỗi tải dữ liệu";
            noRestaurantSection.IsVisible = false;
            managementSection.IsVisible = true;
        }
        finally { _isBusy = false; }
    }

    private async void OnCreateRestaurantClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCreateName.Text) || string.IsNullOrWhiteSpace(txtCreateAddress.Text))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập tên và địa chỉ nhà hàng", "OK");
            return;
        }

        try
        {
            await _authService.SetAuthHeaderAsync();
            var data = new
            {
                Name = txtCreateName.Text.Trim(),
                Address = txtCreateAddress.Text.Trim(),
                Phone = txtCreatePhone.Text?.Trim() ?? "",
                Description = txtCreateDescription.Text?.Trim() ?? "",
                Latitude = 0.0,
                Longitude = 0.0
            };

            var response = await _httpClient.PostAsJsonAsync("api/restaurants", data);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var restaurantId = doc.RootElement.GetProperty("restaurantId").GetInt32();

                var amount = _selectedPlan == "Premium" ? 500000 : 100000;
                var encodedName = Uri.EscapeDataString(txtCreateName.Text.Trim());
                await Shell.Current.GoToAsync(
                    $"{nameof(RestaurantPaymentPage)}?restaurantId={restaurantId}&restaurantName={encodedName}&planType={_selectedPlan}&amount={amount}");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", error, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tạo nhà hàng: " + ex.Message, "OK");
        }
    }

    private void OnSelectNormalPlan(object sender, EventArgs e)
    {
        _selectedPlan = "Normal";
        frameNormalPlan.BackgroundColor = Color.FromArgb("#E3F2FD");
        frameNormalPlan.BorderColor = Color.FromArgb("#1565C0");
        lblNormalCheck.Text = "✅";
        framePremiumPlan.BackgroundColor = Colors.White;
        framePremiumPlan.BorderColor = Color.FromArgb("#E0E0E0");
        lblPremiumCheck.Text = "⬜";
    }

    private void OnSelectPremiumPlan(object sender, EventArgs e)
    {
        _selectedPlan = "Premium";
        framePremiumPlan.BackgroundColor = Color.FromArgb("#FFF8E1");
        framePremiumPlan.BorderColor = Color.FromArgb("#FF8F00");
        lblPremiumCheck.Text = "✅";
        frameNormalPlan.BackgroundColor = Colors.White;
        frameNormalPlan.BorderColor = Color.FromArgb("#E0E0E0");
        lblNormalCheck.Text = "⬜";
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
        await Navigation.PushAsync(new DishListPage(_httpClient, _authService));

    private async void GoEditRestaurant(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("EditRestaurantPage");

    // Điều hướng sang trang MyAudios
    private async void GoMyAudios(object sender, EventArgs e) =>
       await Navigation.PushAsync(new MyAudiosPage(_httpClient, _authService, _audioManager));

    // Điều hướng sang trang Nâng cấp
    private async void GoUpgradePremium(object sender, EventArgs e) =>
        await Navigation.PushAsync(new UpgradePremiumPage(_httpClient, _authService));
}