    using Microsoft.Maui.Devices.Sensors;
    using Plugin.Maui.Audio;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using TourismApp.Services;

    namespace TourismApp;

    public partial class CustomerHomePage : ContentPage
    {
        private readonly RestaurantService _restaurantService;
        private readonly UserService _userService;
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _activePlayer;

        // Sử dụng namespace đầy đủ để tránh xung đột với các Model khác
        public ObservableCollection<TourismApp.Models.Restaurant> Restaurants { get; set; } = new();

        // SỬA LỖI: Thêm IAudioManager vào Constructor để Dependency Injection hoạt động
        public CustomerHomePage(RestaurantService restaurantService, UserService userService, IAudioManager audioManager)
        {
            InitializeComponent();
            _restaurantService = restaurantService;
            _userService = userService;
            _audioManager = audioManager;

            RestaurantList.ItemsSource = Restaurants;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var user = await _userService.GetMeAsync();
                var allRestaurants = await _restaurantService.GetRestaurantsAsync();

                if (allRestaurants == null) return;

                // Lọc nhà hàng đã duyệt
                var filtered = allRestaurants.Where(r => r.IsApproved).ToList();

                // Tính khoảng cách
                try
                {
                    Location location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
                    if (location != null)
                    {
                        foreach (var r in filtered)
                        {
                            r.Distance = Location.CalculateDistance(location.Latitude, location.Longitude, r.Latitude, r.Longitude, DistanceUnits.Kilometers);
                        }
                        filtered = filtered.OrderBy(r => r.Distance).ToList();
                    }
                }
                catch (Exception ex)
                {
                    // Nếu không lấy được vị trí thì bỏ qua bước tính khoảng cách
                    System.Diagnostics.Debug.WriteLine($"Location Error: {ex.Message}");
                }

                Restaurants.Clear();
                foreach (var item in filtered)
                {
                    Restaurants.Add(item);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", ex.Message, "OK");
            }
        }

        // LOGIC PHÁT THUYẾT MINH THEO NGÔN NGỮ MÁY
        private async void OnPlayRestaurantIntro(object sender, EventArgs e)
        {
            var button = sender as Button;
            var restaurant = button?.BindingContext as TourismApp.Models.Restaurant;

            if (restaurant == null || restaurant.Narrations == null || !restaurant.Narrations.Any())
            {
                await DisplayAlert("Thông báo", "Nhà hàng này chưa có bài thuyết minh.", "OK");
                return; 
            }

            try
            {
                // 1. Lấy mã ngôn ngữ máy (vi, en, ja, ko...)
                string deviceLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

                // 2. Tìm bản thuyết minh phù hợp
                var matched = restaurant.Narrations.FirstOrDefault(n => n.Language.Code.ToLower() == deviceLang)
                              ?? restaurant.Narrations.FirstOrDefault(n => n.Language.Code == "en");

                if (matched != null)
                {
                    // Dừng âm thanh đang phát trước đó (nếu có)
                    _activePlayer?.Stop();

                    // TRƯỜNG HỢP 1: CÓ FILE AUDIO TRÊN SERVER
                    if (!string.IsNullOrEmpty(matched.AudioUrl))
                    {
                        // Lưu ý: URL phải trỏ về 10.0.2.2 nếu dùng Android Emulator
                        using var client = new HttpClient();
                        var stream = await client.GetStreamAsync(matched.AudioUrl);

                        _activePlayer = _audioManager.CreatePlayer(stream);
                        _activePlayer.Play();
                    }
                    // TRƯỜNG HỢP 2: CHỈ CÓ VĂN BẢN THÌ DÙNG MÁY ĐỌC
                    else if (!string.IsNullOrEmpty(matched.TextContent))
                    {
                        await TextToSpeech.Default.SpeakAsync(matched.TextContent);
                    }
                }
                else
                {
                    await DisplayAlert("Thông báo", "Không có ngôn ngữ phù hợp.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi phát âm thanh", ex.Message, "OK");
            }
        }

        private async void OnOpenMap(object sender, EventArgs e)
        {
            var restaurant = (sender as Button)?.BindingContext as TourismApp.Models.Restaurant;
            if (restaurant != null) await Navigation.PushAsync(new RestaurantMapPage(restaurant));
        }

        private async void OnRestaurantTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is int id)
                await Shell.Current.GoToAsync($"RestaurantDetailPage?restaurantId={id}");
        }

        private async void OnProfileClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync(nameof(ProfilePage));
    }