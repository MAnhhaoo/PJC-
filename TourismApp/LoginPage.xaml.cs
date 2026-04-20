using System.Net.Http.Json;
using TourismApp.Models;
using TourismApp.Services;

namespace TourismApp;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly LanguageService _lang;
    private readonly HeartbeatService _heartbeat;

    public LoginPage(HttpClient httpClient, AuthService authService, LanguageService languageService, HeartbeatService heartbeatService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
        _lang = languageService;
        _heartbeat = heartbeatService;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        lblWelcome.Text = _lang["Welcome"];
        txtEmail.Placeholder = _lang["EmailPlaceholder"];
        txtPassword.Placeholder = _lang["PasswordPlaceholder"];
        btnLogin.Text = _lang["LoginBtn"];
        btnGuest.Text = _lang["GuestBtn"] ?? "👤 Tiếp tục với tư cách khách";
        lblNoAccount.Text = _lang["NoAccount"];
        btnRegisterNow.Text = _lang["RegisterNow"];
        lblServerSettings.Text = _lang["ServerSettings"];
        lblAuthenticating.Text = _lang["Authenticating"];
    }

    private async void OnGoToRegister(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }

    private async void OnGuestClicked(object sender, EventArgs e)
    {
        // Xóa token và auth header để đảm bảo chế độ khách vãng lai
        await _authService.LogoutAsync();
        _heartbeat.Start();

        if (Shell.Current is AppShell appShell)
        {
            appShell.UpdateMenu("Guest");
        }

        await Shell.Current.GoToAsync("//CustomerHomePage");
    }

    // 1. Chỉ giữ lại DUY NHẤT một hàm này
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert(_lang["Error"], _lang["LoginInputError"], _lang["OK"]);
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
                await DisplayAlert(_lang["Error"], _lang["LoginError"], _lang["TryAgain"]);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            // Lưu token đăng nhập
            await _authService.SaveTokenAsync(result.Token);
            _heartbeat.Start();
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
            await DisplayAlert(_lang["Error"], _lang["ConnectionError"], _lang["Close"]);
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
            _lang["ServerSettingsTitle"],
            _lang["ServerSettingsMsg"],
            initialValue: currentIp,
            keyboard: Keyboard.Url);

        if (!string.IsNullOrWhiteSpace(newIp) && newIp != currentIp)
        {
            Preferences.Default.Set("server_ip", newIp.Trim());
            await DisplayAlert(_lang["ServerSaved"], string.Format(_lang["ServerSavedMsg"], newIp.Trim()), _lang["OK"]);
        }
    }

} // Đóng class LoginPage (Hãy chắc chắn có dấu này ở cuối file)

   