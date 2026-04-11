using Plugin.Maui.Audio;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using TourismApp.Services;
using System.Collections.ObjectModel;
namespace TourismApp;

public partial class MyAudiosPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _activePlayer;
    private CancellationTokenSource _ttsCts;
    private NarrationDto _currentlyPlaying;

    public ObservableCollection<NarrationDto> Narrations { get; set; } = new();
    public MyAudiosPage(HttpClient httpClient, AuthService authService, IAudioManager audioManager)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
        _audioManager = audioManager;

        listAudios.ItemsSource = Narrations;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAudios();
    }

    private async Task LoadAudios()
    {
        try
        {
            await _authService.SetAuthHeaderAsync();
            var res = await _httpClient.GetFromJsonAsync<List<NarrationDto>>("api/restaurants/my/narrations");

            if (res != null)
            {
                Narrations.Clear();
                foreach (var item in res)
                {
                    Narrations.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tải danh sách: " + ex.Message, "OK");
        }
    }

    private async void OnPlayStopClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var narration = (NarrationDto)btn.CommandParameter;
        if (narration == null) return;

        // If this narration is currently playing → stop it
        if (narration.IsPlaying)
        {
            StopCurrentPlayback();
            return;
        }

        // Stop any other playback first
        StopCurrentPlayback();

        // Start playing this narration
        narration.IsPlaying = true;
        _currentlyPlaying = narration;

        try
        {
            bool audioPlayed = false;

            // Try server audio first
            if (!string.IsNullOrEmpty(narration.AudioUrl))
            {
                try
                {
                    var audioStream = await _httpClient.GetStreamAsync(narration.AudioUrl);
                    _activePlayer?.Dispose();
                    _activePlayer = _audioManager.CreatePlayer(audioStream);
                    _activePlayer.PlaybackEnded += (s, args) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() => StopCurrentPlayback());
                    };
                    _activePlayer.Play();
                    audioPlayed = true;
                }
                catch
                {
                    // Server audio failed — will fall back to device TTS below
                }
            }

            // Fallback to device TTS
            if (!audioPlayed && !string.IsNullOrEmpty(narration.TextContent))
            {
                _ttsCts = new CancellationTokenSource();
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                Locale locale = null;
                if (!string.IsNullOrEmpty(narration.LanguageCode))
                {
                    locale = locales.FirstOrDefault(l =>
                        l.Language != null &&
                        l.Language.Equals(narration.LanguageCode, StringComparison.OrdinalIgnoreCase));
                }
                locale ??= locales.FirstOrDefault(l =>
                    l.Name != null &&
                    l.Name.Contains(narration.LanguageName ?? "", StringComparison.OrdinalIgnoreCase));

                await TextToSpeech.Default.SpeakAsync(narration.TextContent,
                    new SpeechOptions { Locale = locale },
                    _ttsCts.Token);

                MainThread.BeginInvokeOnMainThread(() => StopCurrentPlayback());
            }
            else if (!audioPlayed)
            {
                StopCurrentPlayback();
            }
        }
        catch (OperationCanceledException)
        {
            // TTS was cancelled — already handled
        }
        catch (Exception)
        {
            StopCurrentPlayback();
            await DisplayAlert("Thông báo", "Nội dung âm thanh đang được xử lý hoặc lỗi đường truyền.", "OK");
        }
    }

    private void StopCurrentPlayback()
    {
        try { _activePlayer?.Stop(); _activePlayer?.Dispose(); } catch { }
        _activePlayer = null;

        try { _ttsCts?.Cancel(); } catch { }
        _ttsCts = null;

        if (_currentlyPlaying != null)
        {
            _currentlyPlaying.IsPlaying = false;
            _currentlyPlaying = null;
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var narration = (NarrationDto)btn.CommandParameter;

        bool confirm = await DisplayAlert("Xác nhận", $"Xóa bản thuyết minh {narration.LanguageName}?", "Xóa", "Hủy");
        if (confirm)
        {
            try
            {
                if (narration.IsPlaying) StopCurrentPlayback();

                await _authService.SetAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/restaurants/my/narrations/{narration.NarrationId}");
                if (response.IsSuccessStatusCode)
                {
                    Narrations.Remove(narration);
                }
                else
                {
                    await DisplayAlert("Lỗi", "Xóa thất bại", "OK");
                }
            }
            catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCurrentPlayback();
    }

    public class NarrationDto : INotifyPropertyChanged
    {
        public int NarrationId { get; set; }
        public string LanguageName { get; set; }
        public string LanguageCode { get; set; }
        public string TextContent { get; set; }
        public string AudioUrl { get; set; }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayButtonText));
                    OnPropertyChanged(nameof(PlayButtonBackground));
                    OnPropertyChanged(nameof(PlayButtonTextColor));
                }
            }
        }

        public string PlayButtonText => IsPlaying ? "⏹" : "▶";
        public Color PlayButtonBackground => IsPlaying ? Color.FromArgb("#FFEBEE") : Color.FromArgb("#E3F2FD");
        public Color PlayButtonTextColor => IsPlaying ? Color.FromArgb("#D32F2F") : Color.FromArgb("#1565C0");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}