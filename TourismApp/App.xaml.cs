using Microsoft.Extensions.DependencyInjection;
using TourismApp.Services;

namespace TourismApp
{
    public partial class App : Application
    {
        private readonly AuthService _authService;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // 🔥 lấy AuthService từ DI
            _authService = serviceProvider.GetRequiredService<AuthService>();

            // 🔥 load token khi app mở
            Task.Run(async () => await _authService.LoadTokenAsync());
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}