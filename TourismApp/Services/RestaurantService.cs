using System.Net.Http.Json;
using TourismApp.Models;

namespace TourismApp.Services
{
    public class RestaurantService
    {
        private readonly HttpClient _httpClient;

        public RestaurantService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // 🔹 Lấy tất cả nhà hàng
        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<List<Restaurant>>("api/restaurants");
            return response ?? new List<Restaurant>();
        }

        // 🔥 Lấy nhà hàng của chủ quán
        public async Task<Restaurant?> GetMyRestaurantAsync()
        {
            return await _httpClient.GetFromJsonAsync<Restaurant>("api/restaurants/my");
        }

        // ⭐ THÊM HÀM NÀY
        public async Task<Restaurant?> GetRestaurantByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Restaurant>($"api/restaurants/{id}");
        }
    }
}