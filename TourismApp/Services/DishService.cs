using System.Net.Http.Json;
using TourismApp.Models;

namespace TourismApp.Services;

public class DishService
{
    private readonly HttpClient _http;

    public DishService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Dish>> GetDishesByRestaurantAsync(int restaurantId)
    {
        return await _http.GetFromJsonAsync<List<Dish>>(
            $"api/restaurants/{restaurantId}/dishes"
        );
    }
}