using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace TourismApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // 🔐 Lưu token
    public async Task SaveTokenAsync(string token)
    {
        await SecureStorage.SetAsync("auth_token", token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // 🔄 Load lại token khi app mở
    public async Task LoadTokenAsync()
    {
        var token = await SecureStorage.GetAsync("auth_token");

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
    public async Task SetAuthHeaderAsync()
    {
        var token = await SecureStorage.GetAsync("auth_token");

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // 🚪 Logout
    public async Task LogoutAsync()
    {
        SecureStorage.Remove("auth_token");

        _httpClient.DefaultRequestHeaders.Authorization = null;
    }
}