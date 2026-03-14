using Microsoft.Extensions.Logging;
using TourismApp.Services;   // 👈 thêm dòng này

namespace TourismApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
     .UseMauiApp<App>()
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


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}