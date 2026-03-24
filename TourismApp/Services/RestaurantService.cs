using System.Net.Http.Json;

namespace TourismApp.Services;

public class RestaurantService
{
    private readonly HttpClient _httpClient;

    public RestaurantService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TourismApp.Models.Restaurant>> GetRestaurantsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<TourismApp.Models.Restaurant>>("api/restaurants");
        return response ?? new List<TourismApp.Models.Restaurant>();
    }

    public async Task<TourismApp.Models.Restaurant?> GetMyRestaurantAsync()
    {
        return await _httpClient.GetFromJsonAsync<TourismApp.Models.Restaurant>("api/restaurants/my");
    }

    public async Task<TourismApp.Models.Restaurant?> GetRestaurantByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<TourismApp.Models.Restaurant>($"api/restaurants/{id}");
    }
}