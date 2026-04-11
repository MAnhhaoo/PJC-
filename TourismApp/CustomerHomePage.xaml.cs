using Microsoft.Maui.Devices.Sensors;
using System.IO;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using TourismApp.Services;
using TourismApp.Models;
using Microsoft.Maui.Media; // Dòng này cực kỳ quan trọng để hết lỗi đỏ SpeakOptions
using Microsoft.Maui.Storage;

namespace TourismApp;

public partial class CustomerHomePage : ContentPage
{
    private readonly RestaurantService _restaurantService;
    private readonly IAudioManager _audioManager;
    private readonly GpsService _gpsService;
    private IAudioPlayer _activePlayer;
    private Restaurant _currentPlayingRestaurant;
    private bool _isMonitoring = false;
    private readonly HashSet<int> _playedRestaurantIds = new();

    public LanguageService LangService { get; }
    public ObservableCollection<Restaurant> Restaurants { get; set; } = new();

    public CustomerHomePage(RestaurantService restaurantService, UserService userService, IAudioManager audioManager, GpsService gpsService, LanguageService languageService)
    {
        InitializeComponent();
        _restaurantService = restaurantService;
        _audioManager = audioManager;
        _gpsService = gpsService;
        LangService = languageService;

        this.BindingContext = this;
        RestaurantList.ItemsSource = Restaurants;
        StartGpsMonitoring();
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
            var allRestaurants = await _restaurantService.GetRestaurantsAsync();
            if (allRestaurants == null) return;

            var filtered = allRestaurants.Where(r => r.IsApproved).ToList();
            var userLocation = await _gpsService.GetCurrentLocation();

            if (userLocation != null)
            {
                foreach (var r in filtered)
                    r.Distance = _gpsService.CalculateDistance(userLocation.Latitude, userLocation.Longitude, r.Latitude, r.Longitude);

                filtered = filtered.OrderBy(r => r.Distance).ToList();
            }

            Restaurants.Clear();
            foreach (var item in filtered) Restaurants.Add(item);
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    private async void StartGpsMonitoring()
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        while (_isMonitoring)
        {
            try
            {
                var loc = await _gpsService.GetCurrentLocation();
                if (loc != null)
                {
                    foreach (var res in Restaurants)
                    {
                        res.Distance = _gpsService.CalculateDistance(loc.Latitude, loc.Longitude, res.Latitude, res.Longitude);
                        if (res.Distance <= 0.1 && !_playedRestaurantIds.Contains(res.RestaurantId))
                        {
                            _playedRestaurantIds.Add(res.RestaurantId);
                            await PlayRestaurantAudio(res);
                            break;
                        }
                    }
                }
            }
            catch { }
            await Task.Delay(10000);
        }
    }

    private async Task PlayRestaurantAudio(Restaurant restaurant)
    {
        Narration matched = null;
        try
        {
            // 1. Dừng ngay lập tức âm thanh cũ nếu đang phát
            StopCurrentAudio();

            if (restaurant.Narrations == null || !restaurant.Narrations.Any()) return;

            string selectedLang = (LangService.CurrentLanguage ?? "vi").Trim().ToLower();
            matched = restaurant.Narrations.FirstOrDefault(n =>
                n.Language != null && n.Language.Code?.Trim().ToLower() == selectedLang)
                ?? restaurant.Narrations.FirstOrDefault();

            if (matched == null)
                return;

            // If no audio file, fallback to TTS (speak text) so button still works
            if (string.IsNullOrEmpty(matched.AudioUrl))
            {
                if (!string.IsNullOrEmpty(matched.TextContent))
                {
                    try
                    {
                        await TextToSpeech.Default.SpeakAsync(matched.TextContent);
                    }
                    catch { }
                }
                return;
            }

            // 2. Lấy filename an toàn (loại bỏ bất kỳ đường dẫn nào) để tránh 'audios/audios/...'
            var fileName = string.IsNullOrEmpty(matched.AudioUrl) ? string.Empty : Path.GetFileName(matched.AudioUrl);
            if (string.IsNullOrEmpty(fileName)) return;

            var savedIp = Preferences.Default.Get("server_ip", "192.168.1.12");
            var host = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp;
            string audioUrl = $"http://{host}:5216/audios/{fileName}";

            // 3. Thực hiện tải và khởi tạo Player hoàn toàn ở luồng phụ (Fix NetworkOnMainThread)
            await Task.Run(async () =>
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);

                var response = await client.GetAsync(audioUrl);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();

                // Khởi tạo Player ở đây
                var player = _audioManager.CreatePlayer(stream);

                // 4. Quay lại luồng chính ĐỂ PHÁT VÀ CẬP NHẬT UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _activePlayer = player;
                    _currentPlayingRestaurant = restaurant;
                    _currentPlayingRestaurant.IsPlaying = true;

                    // FIX ĐỨNG IM: Khi phát hết nhạc, phải gọi StopCurrentAudio trên luồng chính
                    _activePlayer.PlaybackEnded += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() => StopCurrentAudio());
                    };

                    _activePlayer.Play();
                });
            });
        }
        catch (Exception ex)
        {
            // Nếu lỗi (404, mất mạng...), reset ngay trạng thái UI
            MainThread.BeginInvokeOnMainThread(() => StopCurrentAudio());
            var savedIp2 = Preferences.Default.Get("server_ip", "192.168.1.12");
            var host2 = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : savedIp2;
            var fn = matched != null && !string.IsNullOrEmpty(matched.AudioUrl) ? Path.GetFileName(matched.AudioUrl) : "null";
            await DisplayAlert("Lỗi phát", $"File: http://{host2}:5216/audios/{fn}\nChi tiết: {ex.Message}", "OK");
        }
    }
    private void StopCurrentAudio()
    {
        try
        {
            // 1. Dừng TextToSpeech nếu đang nói
            TextToSpeech.Default.SpeakAsync(string.Empty);

            // 2. Xử lý Player an toàn
            if (_activePlayer != null)
            {
                // Tách biệt việc Stop và Dispose để tránh xung đột luồng
                var playerToDispose = _activePlayer;
                _activePlayer = null; // Gán null ngay để các hàm khác không gọi vào

                if (playerToDispose.IsPlaying)
                    playerToDispose.Stop();

                playerToDispose.Dispose();
            }

            // 3. Cập nhật UI trên luồng chính
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_currentPlayingRestaurant != null)
                {
                    _currentPlayingRestaurant.IsPlaying = false;
                    _currentPlayingRestaurant = null;
                }

                // Reset toàn bộ danh sách để chắc chắn không nút nào bị kẹt trạng thái IsPlaying
                foreach (var res in Restaurants)
                {
                    res.IsPlaying = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi dừng nhạc: {ex.Message}");
        }
    }
    private async void OnSelectLanguageClicked(object sender, EventArgs e)
    {
        // Danh sách hiển thị có lá cờ
        var langItems = new Dictionary<string, string> {
            { "🇻🇳 Tiếng Việt", "vi" },
            { "🇺🇸 English", "en" },
            { "🇯🇵 Japanese", "ja" },
            { "🇫🇷 French", "fr" },
            { "🇰🇷 Korean", "ko" },
            { "🇨🇳 Chinese", "zh" }
        };

        var action = await DisplayActionSheet(LangService["SelectLang"], LangService["Cancel"], null, langItems.Keys.ToArray());

        if (action != null && action != LangService["Cancel"])
        {
            var code = langItems[action];
            LangService.CurrentLanguage = code;
            Preferences.Default.Set("UserLanguage", code);
            await LoadData(); // Load lại để cập nhật
        }
    }

    private async void OnPlayRestaurantIntro(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is Restaurant res) await PlayRestaurantAudio(res);
    }

    private void OnStopAudioClicked(object sender, EventArgs e) => StopCurrentAudio();

    private async void OnOpenMap(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is Restaurant res)
            await Navigation.PushAsync(new RestaurantMapPage(res));
    }

    private async void OnRestaurantTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int id) await Shell.Current.GoToAsync($"RestaurantDetailPage?restaurantId={id}");
    }

    private async void OnProfileClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ProfilePage));

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCurrentAudio();
    }
}