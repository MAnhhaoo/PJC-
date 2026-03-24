using System.Net.Http.Json;

namespace TourismApp;

public partial class DishListPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public DishListPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var dishes = await _httpClient.GetFromJsonAsync<List<Dish>>("api/dishes/my");

        dishList.ItemsSource = dishes;
    }

    private async void OnToggleActive(object sender, ToggledEventArgs e)
    {
        var sw = sender as Switch;
        var dish = sw.BindingContext as Dish;

        var data = new
        {
            isActive = e.Value
        };

        await _httpClient.PutAsJsonAsync($"api/dishes/{dish.DishId}/toggle", data);
    }
}

// model tạm cho FE
public class Dish
{
    public int DishId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public bool IsActive { get; set; }
}