using System;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using TourismApp.Models;

namespace TourismApp;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public LoginPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
            return;
        }

        try
        {
            var loginData = new
            {
                Email = txtEmail.Text.Trim(),
                PasswordHash = txtPassword.Text.Trim()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/users/app-login",
                loginData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Đăng nhập thất bại", error, "OK");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result == null || string.IsNullOrEmpty(result.Token))
            {
                await DisplayAlert("Lỗi", "Không nhận được token", "OK");
                return;
            }

            // 🔐 Lưu JWT
            await SecureStorage.SetAsync("auth_token", result.Token);
            await SecureStorage.SetAsync("user_role", result.Role);

            await DisplayAlert("Thành công", $"Xin chào {result.FullName}", "OK");

            // 🔁 Điều hướng theo Role
            if (result.Role == "Restaurant")
            {
                await Shell.Current.GoToAsync(nameof(RestaurantHomePage));
            }
            else
            {
                await Shell.Current.GoToAsync(nameof(CustomerHomePage));
            }
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Lỗi", "Không kết nối được tới server", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}