using System;
using System.Net.Http;
using System.Net.Http.Json;

namespace TourismApp;

public partial class RegisterPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public RegisterPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text) ||
            string.IsNullOrWhiteSpace(txtFullName.Text) ||
            rolePicker.SelectedItem == null)
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ thông tin", "OK");
            return;
        }

        try
        {
            var registerData = new
            {
                email = txtEmail.Text.Trim(),
                passwordHash = txtPassword.Text.Trim(),
                fullName = txtFullName.Text.Trim(),
                role = rolePicker.SelectedItem?.ToString() ?? "User"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/users/register",
                registerData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Đăng ký thất bại", error, "OK");
                return;
            }

            await DisplayAlert("Thành công", "Đăng ký thành công", "OK");

            // 🔁 quay về login
            await Shell.Current.GoToAsync("..");
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

    private async void OnBackToLogin(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}