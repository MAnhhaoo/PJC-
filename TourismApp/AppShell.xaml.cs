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
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(RestaurantManagerPage), typeof(RestaurantManagerPage));


            Routing.RegisterRoute(nameof(AddDishPage), typeof(AddDishPage));
            Routing.RegisterRoute(nameof(DishListPage), typeof(DishListPage));
            Routing.RegisterRoute(nameof(EditRestaurantPage), typeof(EditRestaurantPage));


            Routing.RegisterRoute(nameof(UpgradePremiumPage), typeof(UpgradePremiumPage));

            Routing.RegisterRoute(nameof(MyAudiosPage), typeof(MyAudiosPage));
        }

        public void UpdateMenu(string role)
        {
            // 1. Xóa các mục cũ (tránh bị lặp Menu)
            var itemsToRemove = Items.Where(i => i is FlyoutItem).ToList();
            foreach (var item in itemsToRemove) Items.Remove(item);

            if (role == "Restaurant")
            {
                // MENU CHO NHÀ HÀNG
                Items.Add(new FlyoutItem
                {
                    Title = "🏠 Quản lý nhà hàng",
                    Items = { new ShellContent { ContentTemplate = new DataTemplate(typeof(RestaurantManagerPage)), Route = "RestaurantManagerPage" } }
                });
                Items.Add(new FlyoutItem
                {
                    Title = "🍱 Thực đơn",
                    Items = { new ShellContent { ContentTemplate = new DataTemplate(typeof(DishListPage)), Route = "DishListPage" } }
                });
                Items.Add(new FlyoutItem
                {
                    Title = "📝 Cập nhật thông tin",
                    Items = { new ShellContent { ContentTemplate = new DataTemplate(typeof(EditRestaurantPage)), Route = "EditRestaurantPage" } }
                });
            }
            else
            {
                // MENU CHO USER THƯỜNG
                Items.Add(new FlyoutItem
                {
                    Title = "📍 Khám phá phố ẩm thực",
                    Items = { new ShellContent { ContentTemplate = new DataTemplate(typeof(CustomerHomePage)), Route = "CustomerHomePage" } }
                });
                Items.Add(new FlyoutItem
                {
                    Title = "👤 Hồ sơ cá nhân",
                    Items = { new ShellContent { ContentTemplate = new DataTemplate(typeof(ProfilePage)), Route = "ProfilePage" } }
                });
            }
        }



        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Xác nhận", "Bạn có chắc chắn muốn đăng xuất?", "Đăng xuất", "Hủy");
            if (confirm)
            {
                // Xóa Token và về trang Login
                Preferences.Default.Remove("jwt_token");

                // Dùng // để reset luồng điều hướng, ngăn việc nhấn Back quay lại trang quản lý
                await Shell.Current.GoToAsync("//LoginPage");

                // Đóng menu vuốt lại sau khi bấm
                Shell.Current.FlyoutIsPresented = false;
            }
        }
    }
}

