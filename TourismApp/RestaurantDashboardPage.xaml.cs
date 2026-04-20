using System.Net.Http.Json;
using System.Text.Json;
using TourismApp.Services;

namespace TourismApp;

public partial class RestaurantDashboardPage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly DishService _dishService;
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private int _restaurantId;
    private string _restaurantName = "";
    private bool _isPremium;

    public RestaurantDashboardPage(
        RestaurantService restaurantService,
        DishService dishService,
        HttpClient httpClient,
        AuthService authService)
    {
        InitializeComponent();
        _restaurantService = restaurantService;
        _dishService = dishService;
        _httpClient = httpClient;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var data = await _restaurantService.GetMyRestaurantAsync();

        if (data == null)
        {
            await DisplayAlert("Lỗi", "Bạn chưa đăng ký nhà hàng", "OK");
            return;
        }

        _restaurantId = data.RestaurantId;
        _restaurantName = data.Name ?? "";
        _isPremium = data.IsPremium;

        var dishes = await _dishService.GetDishesByRestaurantAsync(data.RestaurantId);

        BindingContext = new
        {
            data.Image,
            data.Name,
            data.Address,
            data.Description,
            PremiumText = data.IsPremium && data.PremiumExpireDate != null
                ? $"⭐ Premium đến {data.PremiumExpireDate.Value.ToString("dd/MM/yyyy")}"
                : "Tài khoản thường",
            Dishes = dishes
        };

        // Show dish limit info
        var dishCount = dishes?.Count ?? 0;
        if (!data.IsPremium)
        {
            frameDishLimit.IsVisible = true;
            frameDishLimit.BackgroundColor = dishCount >= 8
                ? Color.FromArgb("#FFEBEE")
                : Color.FromArgb("#E3F2FD");
            lblDishLimit.TextColor = dishCount >= 8
                ? Color.FromArgb("#C62828")
                : Color.FromArgb("#1565C0");
            lblDishLimit.Text = $"🍽 Món ăn: {dishCount}/8 — Nâng cấp Premium để không giới hạn";
        }
        else
        {
            frameDishLimit.IsVisible = true;
            frameDishLimit.BackgroundColor = Color.FromArgb("#E8F5E9");
            lblDishLimit.TextColor = Color.FromArgb("#2E7D32");
            lblDishLimit.Text = $"⭐ Premium — {dishCount} món (không giới hạn)";
        }
    }

    private async void OnEditRestaurantClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRestaurantPage));
    }

    private async void OnManageNarrationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyAudiosPage));
    }

    private async void OnDishListClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DishListPage));
    }

    private async void OnAddDishClicked(object sender, EventArgs e)
    {
        // Check dish limit for non-premium restaurants
        if (!_isPremium)
        {
            try
            {
                await _authService.SetAuthHeaderAsync();
                var response = await _httpClient.GetAsync($"api/payments/restaurant-dish-limit/{_restaurantId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var canAdd = doc.RootElement.GetProperty("canAddMore").GetBoolean();
                    var current = doc.RootElement.GetProperty("currentCount").GetInt32();

                    if (!canAdd)
                    {
                        var upgrade = await DisplayAlert("Giới hạn món ăn",
                            $"Nhà hàng thường chỉ được tối đa 8 món ({current}/8).\n\nNâng cấp Premium để thêm không giới hạn!",
                            "⭐ Nâng cấp", "Đóng");
                        if (upgrade)
                            await Shell.Current.GoToAsync(nameof(UpgradePremiumPage));
                        return;
                    }
                }
            }
            catch { }
        }

        await Shell.Current.GoToAsync(nameof(AddDishPage));
    }

    private async void OnUpgradeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UpgradePremiumPage));
    }

    private async void OnQRCodeClicked(object sender, EventArgs e)
    {
        if (_restaurantId <= 0)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy thông tin nhà hàng", "OK");
            return;
        }
        var encodedName = Uri.EscapeDataString(_restaurantName);
        await Shell.Current.GoToAsync($"{nameof(RestaurantQRPage)}?restaurantId={_restaurantId}&restaurantName={encodedName}");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Đăng xuất", "Hủy");
        if (!confirm) return;

        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}