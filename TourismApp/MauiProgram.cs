using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using TourismApp.Services;   
namespace TourismApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Nếu xóa dòng này mà hết lỗi thì nên xóa
                .UseMauiMaps()

                 .ConfigureFonts(fonts =>
     {
         fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
     });

            // 🔥 HttpClient cho Android Emulator
            builder.Services.AddSingleton<HttpClient>(s =>
            {
                return new HttpClient
                {
                    BaseAddress = new Uri("http://10.0.2.2:5216/")
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
            builder.Services.AddTransient<AddDishPage>();
            builder.Services.AddTransient<DishListPage>();
            builder.Services.AddTransient<EditRestaurantPage>();
            builder.Services.AddTransient<UpgradePremiumPage>();

            builder.Services.AddTransient<MyAudiosPage>(); // Đăng ký trang Audio
            builder.Services.AddTransient<UpgradePremiumPage>();
            builder.Services.AddSingleton<LanguageService>(); // Đăng ký dạng Singleton để lưu app state



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}