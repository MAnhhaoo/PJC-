using System.Collections.ObjectModel;
using System.Net.Http.Json;
using TourismApp.Services; // Đảm bảo có cái này để dùng AuthService
using TourismApp.Models;
namespace TourismApp;

public partial class DishListPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService; // Thêm vào
    public ObservableCollection<Dish> Dishes { get; set; } = new();
    private bool _isInitialLoading = true; // Cờ ngăn lỗi lặp vô tận

    public DishListPage(HttpClient httpClient, AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
        dishList.ItemsSource = Dishes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDishes();
    }

    private async Task LoadDishes()
    {
        try
        {
            _isInitialLoading = true; // Bắt đầu load
            await _authService.SetAuthHeaderAsync();
            var dishes = await _httpClient.GetFromJsonAsync<List<Dish>>("api/dishes/my");

            Dishes.Clear();
            if (dishes != null)
            {
                foreach (var dish in dishes) Dishes.Add(dish);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tải món ăn: " + ex.Message, "OK");
        }
        finally
        {
            _isInitialLoading = false; // Kết thúc load
        }
    }

    private async void OnToggleActive(object sender, ToggledEventArgs e)
    {
        // Nếu đang trong quá trình load dữ liệu ban đầu thì không gọi API
        if (_isInitialLoading) return;

        var sw = sender as Switch;
        var dish = sw.BindingContext as Dish;
        if (dish == null) return;

        try
        {
            var data = new { isActive = e.Value };
            await _authService.SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync($"api/dishes/{dish.DishId}/toggle", data);

            if (!response.IsSuccessStatusCode)
            {
                // Nếu lỗi, trả lại trạng thái cũ trên giao diện
                _isInitialLoading = true;
                sw.IsToggled = !e.Value;
                _isInitialLoading = false;
                await DisplayAlert("Lỗi", "Không thể cập nhật trạng thái", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}