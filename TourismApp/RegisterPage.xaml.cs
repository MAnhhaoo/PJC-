using System;
using System.Net.Http;
using System.Net.Http.Json;
using TourismApp.Services;

namespace TourismApp;

public partial class RegisterPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly LanguageService _lang;

    public RegisterPage(HttpClient httpClient, LanguageService languageService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _lang = languageService;
        Title = _lang["RegisterTitle"];
        lblRegTitle.Text = _lang["RegisterTitle"];
        txtFullName.Placeholder = _lang["FullNamePlaceholder"];
        txtEmail.Placeholder = _lang["Email"];
        txtPassword.Placeholder = _lang["PasswordPlaceholder"];
        rolePicker.Title = _lang["ChooseAccountType"];
        btnRegister.Text = _lang["RegisterBtn"];
        btnBackToLogin.Text = _lang["BackToLogin"];
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmail.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text) ||
            string.IsNullOrWhiteSpace(txtFullName.Text) ||
            rolePicker.SelectedItem == null)
        {
            await DisplayAlert(_lang["Error"], _lang["RegisterInputError"], _lang["OK"]);
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
                await DisplayAlert(_lang["RegisterFailed"], error, _lang["OK"]);
                return;
            }

            await DisplayAlert(_lang["Success"], _lang["RegisterSuccess"], _lang["OK"]);

            // 🔁 quay về login
            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert(_lang["Error"], _lang["NoServerConnection"], _lang["OK"]);
        }
        catch (Exception ex)
        {
            await DisplayAlert(_lang["Error"], ex.Message, _lang["OK"]);
        }
    }

    private async void OnBackToLogin(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}