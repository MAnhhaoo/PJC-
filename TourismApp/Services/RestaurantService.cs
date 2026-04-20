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
        var list = response ?? new List<TourismApp.Models.Restaurant>();
        foreach (var r in list)
            r.Image = ResolveUrl(r.Image);
        return list;
    }

    public async Task<TourismApp.Models.Restaurant?> GetMyRestaurantAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<TourismApp.Models.Restaurant>("api/restaurants/my");
        if (result != null) result.Image = ResolveUrl(result.Image);
        return result;
    }

    public async Task<TourismApp.Models.Restaurant?> GetRestaurantByIdAsync(int id)
    {
        var result = await _httpClient.GetFromJsonAsync<TourismApp.Models.Restaurant>($"api/restaurants/{id}");
        if (result != null) result.Image = ResolveUrl(result.Image);
        return result;
    }

    public string ResolveUrl(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (value.StartsWith("http"))
        {
            try
            {
                var uri = new Uri(value);
                // External URL (different host from our server) — return as-is
                if (_httpClient.BaseAddress == null || !string.Equals(uri.Host, _httpClient.BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
                    return value;
                return new Uri(_httpClient.BaseAddress!, uri.PathAndQuery).ToString();
            }
            catch { return value; }
        }
        return new Uri(_httpClient.BaseAddress!, value).ToString();
    }
}