using TourismApp.Services;

namespace TourismApp;

public partial class RestaurantDashboardPage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly DishService _dishService;

    public RestaurantDashboardPage(
        RestaurantService restaurantService,
        DishService dishService)
    {
        InitializeComponent();
        _restaurantService = restaurantService;
        _dishService = dishService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var data = await _restaurantService.GetMyRestaurantAsync();
        var dishes = await _dishService.GetDishesByRestaurantAsync(data.RestaurantId);

        BindingContext = new
        {
            data.Image,
            data.Name,
            data.Address,
            data.Description,
            PremiumText = data.IsPremium
                ? $"⭐ Premium until {data.PremiumExpireDate}"
                : "Normal Account",
            Dishes = dishes
        };
    }

    private async void OnUpgradeClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Upgrade", "Redirect to payment screen", "OK");
    }
}