using System.Net.Http.Json;
using TourismApp.Services;

namespace TourismApp;

public partial class UpgradePremiumPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    public UpgradePremiumPage(HttpClient httpClient , AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
    }

    // Gói Silver - Level 1
    private async void OnUpgradeSilverClicked(object sender, EventArgs e)
    {
        await ProcessUpgrade(1, "SILVER (Tháng)", "50.000đ");
    }

    // Gói Gold - Level 2
    private async void OnUpgradeGoldClicked(object sender, EventArgs e)
    {
        await ProcessUpgrade(2, "GOLD (Năm)", "500.000đ");
    }

    private async Task ProcessUpgrade(int level, string packageName, string price)
    {
        bool confirm = await DisplayAlert("Xác nhận", $"Bạn muốn nâng cấp gói {packageName} với giá {price}?", "Đồng ý", "Hủy");

        if (confirm)
        {
            try
            {
                // Gọi tới API: [HttpPatch("upgrade/{level}")] ở Backend
                var response = await _httpClient.PatchAsync($"api/restaurants/upgrade/{level}", null);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Thành công", $"Chúc mừng! Bạn đã nâng cấp lên gói {packageName} thành công.", "Tuyệt vời");
                    await Shell.Current.GoToAsync(".."); // Quay lại trang trước
                }
                else
                {
                    await DisplayAlert("Lỗi", "Không thể thực hiện nâng cấp. Vui lòng thử lại sau.", "Đóng");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi kết nối", ex.Message, "Đóng");
            }
        }
    }
}