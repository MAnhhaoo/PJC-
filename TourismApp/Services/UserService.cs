using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourismApp.Models;

namespace TourismApp.Services;

public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task AddJwt()
    {
        var token = await SecureStorage.GetAsync("auth_token");

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<User> GetMeAsync()
    {
        await AddJwt();
        return await _httpClient.GetFromJsonAsync<User>("api/users/me");
    }

    public async Task<bool> UpdateMeAsync(User user)
    {
        await AddJwt();

        var response = await _httpClient.PutAsJsonAsync(
            "api/users/me",
            new
            {
                user.FullName,
                user.Phone,
                user.Address
            });

        return response.IsSuccessStatusCode;
    }
}