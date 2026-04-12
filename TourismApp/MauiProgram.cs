using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TourismApp.Services;
using ZXing.Net.Maui.Controls;
namespace TourismApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiMaps()
                .UseBarcodeReader()

                 .ConfigureFonts(fonts =>
     {
         fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
     });

            // 🔥 Auto-detect: máy ảo dùng 10.0.2.2, điện thoại thật dùng IP WiFi (lưu trong Preferences)
            var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
            var baseUrl = DeviceInfo.DeviceType == DeviceType.Virtual
                ? "http://10.0.2.2:5216/"
                : $"http://{savedIp}:5216/";
            builder.Services.AddSingleton<HttpClient>(s =>
            {
                return new HttpClient
                {
                    BaseAddress = new Uri(baseUrl)
                };
            });
            // Thay dòng cũ bằng dòng này để đảm bảo DI tìm đúng IAudioManager
            builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);
            // Thêm dòng này vào cụm Services trong MauiProgram.cs
            builder.Services.AddSingleton<GpsService>();
            builder.Services.AddSingleton<AuthService>();



            // 👇👇👇 THÊM 2 DÒNG NÀY
            builder.Services.AddSingleton<RestaurantService>();
            builder.Services.AddTransient<CustomerHomePage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<EditProfilePage>();
            builder.Services.AddSingleton<DishService>();
            builder.Services.AddTransient<RestaurantDetailPage>();
            builder.Services.AddTransient<RestaurantMapPage>();
            builder.Services.AddTransient<RegisterPage>();


            builder.Services.AddTransient<RestaurantDashboardPage>();
            builder.Services.AddTransient<POIMapPage>();
            builder.Services.AddTransient<AddDishPage>();
            builder.Services.AddTransient<DishListPage>();
            builder.Services.AddTransient<EditRestaurantPage>();
            builder.Services.AddTransient<UpgradePremiumPage>();

            builder.Services.AddTransient<MyAudiosPage>(); // Đăng ký trang Audio
            builder.Services.AddTransient<UpgradePremiumPage>();
            builder.Services.AddSingleton<LanguageService>(); // Đăng ký dạng Singleton để lưu app state
            builder.Services.AddTransient<QRScannerPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<RestaurantQRPage>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}