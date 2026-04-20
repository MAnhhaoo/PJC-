using System.Net.Http.Json;
using TourismApp.Services;

namespace TourismApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(RestaurantHomePage), typeof(RestaurantHomePage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
            Routing.RegisterRoute(nameof(RestaurantDashboardPage), typeof(RestaurantDashboardPage));
            Routing.RegisterRoute(nameof(RestaurantDetailPage), typeof(RestaurantDetailPage));
            Routing.RegisterRoute(nameof(RestaurantMapPage), typeof(RestaurantMapPage));

            Routing.RegisterRoute(nameof(AddDishPage), typeof(AddDishPage));
            Routing.RegisterRoute(nameof(DishListPage), typeof(DishListPage));
            Routing.RegisterRoute(nameof(EditRestaurantPage), typeof(EditRestaurantPage));

            Routing.RegisterRoute(nameof(UpgradePremiumPage), typeof(UpgradePremiumPage));

            Routing.RegisterRoute(nameof(MyAudiosPage), typeof(MyAudiosPage));
            Routing.RegisterRoute(nameof(QRScannerPage), typeof(QRScannerPage));
            Routing.RegisterRoute(nameof(RestaurantQRPage), typeof(RestaurantQRPage));
            Routing.RegisterRoute(nameof(TourListPage), typeof(TourListPage));
            Routing.RegisterRoute(nameof(TourDetailPage), typeof(TourDetailPage));
            Routing.RegisterRoute(nameof(TourPaymentPage), typeof(TourPaymentPage));
            Routing.RegisterRoute(nameof(RestaurantPaymentPage), typeof(RestaurantPaymentPage));
        }

        public void UpdateMenu(string role)
        {
            // TabBar-based navigation handles routing via //CustomerHomePage, //RestaurantManagerPage
            // No FlyoutItems needed since FlyoutBehavior=Disabled
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Xác nhận", "Bạn có chắc chắn muốn đăng xuất?", "Đăng xuất", "Hủy");
            if (confirm)
            {
                try
                {
                    var heartbeat = Handler?.MauiContext?.Services.GetService<HeartbeatService>();
                    heartbeat?.Stop();
                }
                catch { }

                // Notify server to clear online status immediately
                try
                {
                    var httpClient = Handler?.MauiContext?.Services.GetService<HttpClient>();
                    if (httpClient != null)
                    {
                        var token = await SecureStorage.GetAsync("auth_token");
                        if (!string.IsNullOrEmpty(token))
                        {
                            await httpClient.PostAsync("api/users/logout", null);
                        }
                        else
                        {
                            var deviceId = Preferences.Default.Get("guest_device_id", "");
                            if (!string.IsNullOrEmpty(deviceId))
                            {
                                await httpClient.PostAsJsonAsync("api/users/guest-logout",
                                    new { DeviceId = deviceId });
                            }
                        }
                    }
                }
                catch { }

                SecureStorage.Remove("auth_token");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}

