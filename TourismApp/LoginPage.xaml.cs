using System.Net.Http.Json;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public LoginPage(HttpClient httpClient, AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    // 1. Chỉ giữ lại DUY NHẤT một hàm này
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ email và mật khẩu", "OK");
            return;
        }

        // Hiện màn hình chờ và khóa nút bấm
        loadingOverlay.IsVisible = true;
        btnLogin.IsEnabled = false;

        try
        {
            var loginData = new { Email = txtEmail.Text.Trim(), PasswordHash = txtPassword.Text.Trim() };
            var response = await _httpClient.PostAsJsonAsync("api/users/app-login", loginData);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Lỗi", "Tài khoản hoặc mật khẩu không đúng", "Thử lại");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            // Lưu token đăng nhập
            await _authService.SaveTokenAsync(result.Token);
            if (Shell.Current is AppShell appShell)
            {
                appShell.UpdateMenu(result.Role);
            }

            // Điều hướng sang trang chủ (Reset luồng để hiện Flyout Menu)
            if (result.Role == "Restaurant")
            {
                await Shell.Current.GoToAsync("//RestaurantManagerPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//CustomerHomePage");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi kết nối", "Không thể kết nối đến máy chủ", "Đóng");
        }
        finally
        {
            // Tắt màn hình chờ sau khi hoàn tất
            loadingOverlay.IsVisible = false;
            btnLogin.IsEnabled = true;
        }
    } // Đóng hàm OnLoginClicked

    private async void OnChangeServerIp(object sender, EventArgs e)
    {
        var currentIp = Preferences.Default.Get("server_ip", "192.168.1.12");
        var newIp = await DisplayPromptAsync(
            "Cài đặt máy chủ",
            "Nhập địa chỉ IP WiFi của máy tính\n(chạy 'ipconfig' trên máy tính để xem):",
            initialValue: currentIp,
            keyboard: Keyboard.Url);

        if (!string.IsNullOrWhiteSpace(newIp) && newIp != currentIp)
        {
            Preferences.Default.Set("server_ip", newIp.Trim());
            await DisplayAlert("Đã lưu", $"IP máy chủ: {newIp.Trim()}\nVui lòng tắt và mở lại app để áp dụng.", "OK");
        }
    }

} // Đóng class LoginPage (Hãy chắc chắn có dấu này ở cuối file)

   