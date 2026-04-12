using TourismApp.Services;

namespace TourismApp;

public partial class RestaurantDashboardPage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly DishService _dishService;
    private int _restaurantId;
    private string _restaurantName = "";

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

        if (data == null)
        {
            await DisplayAlert("Lỗi", "Bạn chưa đăng ký nhà hàng", "OK");
            return;
        }

        _restaurantId = data.RestaurantId;
        _restaurantName = data.Name ?? "";

        var dishes = await _dishService.GetDishesByRestaurantAsync(data.RestaurantId);

        BindingContext = new
        {
            data.Image,
            data.Name,
            data.Address,
            data.Description,
            PremiumText = data.IsPremium && data.PremiumExpireDate != null
                ? $"⭐ Premium đến {data.PremiumExpireDate.Value.ToString("dd/MM/yyyy")}"
                : "Tài khoản thường",
            Dishes = dishes
        };
    }

    private async void OnEditRestaurantClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EditRestaurantPage));
    }

    private async void OnManageNarrationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyAudiosPage));
    }

    private async void OnDishListClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DishListPage));
    }

    private async void OnAddDishClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddDishPage));
    }

    private async void OnUpgradeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UpgradePremiumPage));
    }

    private async void OnQRCodeClicked(object sender, EventArgs e)
    {
        if (_restaurantId <= 0)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy thông tin nhà hàng", "OK");
            return;
        }
        var encodedName = Uri.EscapeDataString(_restaurantName);
        await Shell.Current.GoToAsync($"{nameof(RestaurantQRPage)}?restaurantId={_restaurantId}&restaurantName={encodedName}");
    }
}