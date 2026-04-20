using Microsoft.Extensions.DependencyInjection;
using TourismApp.Services;

namespace TourismApp
{
    public partial class App : Application
    {
        private readonly AuthService _authService;
        private readonly HeartbeatService _heartbeatService;
        private static App _instance;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _instance = this;

            // 🔥 lấy AuthService từ DI
            _authService = serviceProvider.GetRequiredService<AuthService>();
            _heartbeatService = serviceProvider.GetRequiredService<HeartbeatService>();
        }

        /// <summary>
        /// Called from MainActivity.OnNewIntent when app is already running and receives a deep link
        /// </summary>
        public static void NavigateToDeepLink()
        {
            // If external QR → force guest mode by clearing auth token
            var forceGuest = Preferences.Default.Get("deeplink_guest_mode", false);
            if (forceGuest)
            {
                Preferences.Default.Remove("deeplink_guest_mode");
                try { SecureStorage.Remove("auth_token"); } catch { }
            }

            _instance?.NavigateToRestaurantIfNeeded();
        }

        private void NavigateToRestaurantIfNeeded()
        {
            var restaurantId = Preferences.Default.Get("deeplink_restaurant_id", -1);
            if (restaurantId > 0)
            {
                Preferences.Default.Remove("deeplink_restaurant_id");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(
                            $"{nameof(RestaurantDetailPage)}?restaurantId={restaurantId}");
                    }
                    catch { }
                });
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Handle auto-login and deep link navigation after app is ready
            window.Created += (s, e) =>
            {
                _ = HandleStartupAsync();
            };

            return window;
        }

        private async Task HandleStartupAsync()
        {
            // Small delay to ensure Shell is fully initialized
            await Task.Delay(800);

            // Check for deep link first
            var forceGuest = Preferences.Default.Get("deeplink_guest_mode", false);
            if (forceGuest)
            {
                Preferences.Default.Remove("deeplink_guest_mode");
                try { SecureStorage.Remove("auth_token"); } catch { }
            }

            var restaurantId = Preferences.Default.Get("deeplink_restaurant_id", -1);
            if (restaurantId > 0)
            {
                Preferences.Default.Remove("deeplink_restaurant_id");
                _heartbeatService.Start();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        // Go to customer home first, then navigate to detail
                        await Shell.Current.GoToAsync("//CustomerHomePage");
                        await Shell.Current.GoToAsync(
                            $"{nameof(RestaurantDetailPage)}?restaurantId={restaurantId}");
                    }
                    catch { }
                });
                return;
            }

            // Auto-login if token exists
            if (!forceGuest)
            {
                var token = await SecureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    await _authService.LoadTokenAsync();
                    _heartbeatService.Start();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            // Determine role from token or just go to customer home
                            await Shell.Current.GoToAsync("//CustomerHomePage");
                        }
                        catch { }
                    });
                }
            }
        }
    }
}