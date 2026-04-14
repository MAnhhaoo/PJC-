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
    private readonly AnalyticsService _analyticsService;
    private readonly OfflineSyncService _offlineSyncService;
    private readonly TranslationService _translationService;
    private IAudioPlayer _activePlayer;
    private Restaurant _currentPlayingRestaurant;
    private bool _isMonitoring = false;
    private readonly HashSet<int> _playedRestaurantIds = new();
    private bool _isSpeaking = false;
    private CancellationTokenSource _ttsCts;
    private double GeofenceRadiusMeters => Preferences.Default.Get("geofence_radius", 5.0);
    private double MinMovementMeters => Preferences.Default.Get("min_movement", 3.0);
    private Location _lastTriggerLocation;

    public LanguageService LangService { get; }
    public ObservableCollection<Restaurant> Restaurants { get; set; } = new();

    public CustomerHomePage(RestaurantService restaurantService, UserService userService, IAudioManager audioManager, GpsService gpsService, LanguageService languageService, AnalyticsService analyticsService, OfflineSyncService offlineSyncService, TranslationService translationService)
    {
        InitializeComponent();
        _restaurantService = restaurantService;
        _audioManager = audioManager;
        _gpsService = gpsService;
        _analyticsService = analyticsService;
        _offlineSyncService = offlineSyncService;
        _translationService = translationService;
        LangService = languageService;

        this.BindingContext = this;
        RestaurantList.ItemsSource = Restaurants;
        StartGpsMonitoring();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
        StartGpsMonitoring();
    }

    private async Task LoadData()
    {
        try
        {
            List<TourismApp.Models.Restaurant> allRestaurants;

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    await _offlineSyncService.SyncRestaurantsAsync();
                    await _offlineSyncService.DownloadAudioFilesAsync();
                    allRestaurants = await _offlineSyncService.GetRestaurantsOfflineAsync();
                }
                catch
                {
                    allRestaurants = await _offlineSyncService.GetRestaurantsOfflineAsync();
                }
            }
            else
            {
                allRestaurants = await _offlineSyncService.GetRestaurantsOfflineAsync();
            }

            if (allRestaurants == null || allRestaurants.Count == 0) return;

            var filtered = allRestaurants.Where(r => r.IsApproved).ToList();
            var userLocation = await _gpsService.GetCurrentLocation();

            if (userLocation != null)
            {
                foreach (var r in filtered)
                    r.Distance = _gpsService.CalculateDistance(userLocation.Latitude, userLocation.Longitude, r.Latitude, r.Longitude);

                filtered = filtered.OrderBy(r => r.Distance).ToList();
            }

            // Translate restaurant data if language is not Vietnamese
            var lang = (LangService.CurrentLanguage ?? "vi").Trim().ToLower();
            if (lang != "vi")
                await _translationService.TranslateRestaurantsAsync(filtered, lang);

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
                        res.Distance = _gpsService.CalculateDistance(loc.Latitude, loc.Longitude, res.Latitude, res.Longitude);

                    // Geofence Engine: find nearest unplayed restaurant within radius, auto-play if idle
                    if (_activePlayer == null && _currentPlayingRestaurant == null && !_isSpeaking)
                    {
                        var nearest = Restaurants
                            .Where(r => r.Distance <= GeofenceRadiusMeters && !_playedRestaurantIds.Contains(r.RestaurantId))
                            .OrderBy(r => r.Distance)
                            .FirstOrDefault();

                        if (nearest != null)
                        {
                            // Only trigger if user has moved since last narration (actually walked to this restaurant)
                            bool shouldTrigger = _lastTriggerLocation == null
                                || _gpsService.CalculateDistance(loc.Latitude, loc.Longitude,
                                    _lastTriggerLocation.Latitude, _lastTriggerLocation.Longitude) >= MinMovementMeters;

                            if (shouldTrigger)
                            {
                                _lastTriggerLocation = loc;
                                _playedRestaurantIds.Add(nearest.RestaurantId);
                                await PlayRestaurantAudio(nearest);
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.Delay(5000);
        }
    }

    private async Task PlayRestaurantAudio(Restaurant restaurant)
    {
        _playedRestaurantIds.Add(restaurant.RestaurantId);
        Narration matched = null;
        try
        {
            // 1. Dừng ngay lập tức âm thanh cũ nếu đang phát
            StopCurrentAudio();

            if (restaurant.Narrations == null || !restaurant.Narrations.Any()) return;

            string selectedLang = (LangService.CurrentLanguage ?? "vi").Trim().ToLower();
            matched = restaurant.Narrations.FirstOrDefault(n =>
                n.Language != null && n.Language.Code?.Trim().ToLower() == selectedLang);

            if (matched == null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                    await DisplayAlert(LangService["Notification"], LangService["NoNarrationLang"], LangService["OK"]));
                return;
            }

            _ = _analyticsService.LogNarrationPlayAsync(
                restaurant.RestaurantId, null, matched.NarrationId,
                matched.Language?.Code ?? selectedLang, restaurant.Latitude, restaurant.Longitude);

            // If no audio file, fallback to TTS (speak text) so button still works
            if (string.IsNullOrEmpty(matched.AudioUrl))
            {
                if (!string.IsNullOrEmpty(matched.TextContent))
                {
                    _isSpeaking = true;
                    _currentPlayingRestaurant = restaurant;
                    restaurant.IsPlaying = true;
                    _ttsCts?.Cancel();
                    _ttsCts = new CancellationTokenSource();
                    try
                    {
                        await TextToSpeech.Default.SpeakAsync(matched.TextContent, cancelToken: _ttsCts.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                    finally
                    {
                        _isSpeaking = false;
                        restaurant.IsPlaying = false;
                        _currentPlayingRestaurant = null;
                    }
                }
                return;
            }

            // 2. Lấy filename an toàn (loại bỏ bất kỳ đường dẫn nào) để tránh 'audios/audios/...'
            var fileName = string.IsNullOrEmpty(matched.AudioUrl) ? string.Empty : Path.GetFileName(matched.AudioUrl);
            if (string.IsNullOrEmpty(fileName)) return;

            // Ưu tiên phát file offline nếu đã tải
            if (!string.IsNullOrEmpty(matched.LocalAudioPath) && File.Exists(matched.LocalAudioPath))
            {
                var localStream = File.OpenRead(matched.LocalAudioPath);
                var localPlayer = _audioManager.CreatePlayer(localStream);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _activePlayer = localPlayer;
                    _currentPlayingRestaurant = restaurant;
                    _currentPlayingRestaurant.IsPlaying = true;
                    _activePlayer.PlaybackEnded += (s, e) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() => StopCurrentAudio());
                    };
                    _activePlayer.Play();
                });
                return;
            }

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
            _ttsCts?.Cancel();

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
        var langItems = LangService.AvailableLanguages;

        var action = await DisplayActionSheet(LangService["SelectLang"], LangService["Cancel"], null, langItems.Keys.ToArray());

        if (action != null && action != LangService["Cancel"] && langItems.ContainsKey(action))
        {
            var code = langItems[action];
            LangService.CurrentLanguage = code;
            Preferences.Default.Set("UserLanguage", code);
            await LoadData();
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
        _isMonitoring = false;
        _lastTriggerLocation = null;
        StopCurrentAudio();
    }
}