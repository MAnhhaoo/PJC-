using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace TourismApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

    // Deep link: tourismapp://restaurant/{id}
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "tourismapp",
        DataHost = "restaurant")]

    // Deep link: tourismapp://home — opens app home page
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "tourismapp",
        DataHost = "home")]

    // Deep link: http(s)://*.*/r/{id} — for external camera QR scanning
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "http",
        DataPathPrefix = "/r/",
        AutoVerify = false)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataPathPrefix = "/r/",
        AutoVerify = false)]

    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            HandleDeepLink(intent);
            // Notify the app to navigate immediately (app is already running)
            App.NavigateToDeepLink();
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleDeepLink(Intent);
        }

        private void HandleDeepLink(Intent? intent)
        {
            if (intent?.Data == null) return;

            var uri = intent.Data;
            int restaurantId = -1;
            bool isExternalQR = false;

            // tourismapp://home — just open the app (goes to home by default)
            if (uri.Scheme == "tourismapp" && uri.Host == "home")
            {
                // App opens to customer home page by default, no special action needed
                return;
            }

            // tourismapp://restaurant/{id}
            if (uri.Scheme == "tourismapp" && uri.Host == "restaurant")
            {
                var idStr = uri.Path?.TrimStart('/');
                int.TryParse(idStr, out restaurantId);
            }
            // http(s)://.../r/{id} — external camera QR scan
            else if ((uri.Scheme == "http" || uri.Scheme == "https")
                     && uri.Path != null && uri.Path.StartsWith("/r/"))
            {
                var idStr = uri.Path.Substring(3); // remove "/r/"
                int.TryParse(idStr, out restaurantId);
                isExternalQR = true;
            }

            if (restaurantId > 0)
            {
                // External QR scan → force guest mode (clear logged-in session)
                if (isExternalQR)
                {
                    Preferences.Default.Set("deeplink_guest_mode", true);
                }

                // Store the deep link restaurant ID so the app can navigate after init
                Preferences.Default.Set("deeplink_restaurant_id", restaurantId);
            }
        }
    }
}
