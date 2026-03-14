namespace TourismApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(RestaurantHomePage), typeof(RestaurantHomePage));
            Routing.RegisterRoute(nameof(CustomerHomePage), typeof(CustomerHomePage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
            Routing.RegisterRoute(nameof(RestaurantDashboardPage), typeof(RestaurantDashboardPage));
            Routing.RegisterRoute(nameof(RestaurantDetailPage), typeof(RestaurantDetailPage));
            Routing.RegisterRoute(nameof(RestaurantMapPage), typeof(RestaurantMapPage));

        }
    }
}