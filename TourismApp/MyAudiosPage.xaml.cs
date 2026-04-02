using Plugin.Maui.Audio;
using System.Net.Http.Json;
using TourismApp.Services;
using System.Collections.ObjectModel;
namespace TourismApp;

public partial class MyAudiosPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _activePlayer;

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
    private async void OnPlayClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var narration = (NarrationDto)btn.CommandParameter;
        if (narration == null) return;

        try
        {
            if (!string.IsNullOrEmpty(narration.AudioUrl))
            {
                // Gọi API lấy luồng âm thanh
                var audioStream = await _httpClient.GetStreamAsync(narration.AudioUrl);
                _activePlayer?.Dispose();
                _activePlayer = _audioManager.CreatePlayer(audioStream);
                _activePlayer.Play();
            }
            else if (!string.IsNullOrEmpty(narration.TextContent))
            {
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Name.Contains(narration.LanguageName, StringComparison.OrdinalIgnoreCase));

                await TextToSpeech.Default.SpeakAsync(narration.TextContent, new SpeechOptions { Locale = locale });
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Thông báo", "Nội dung âm thanh đang được xử lý hoặc lỗi đường truyền.", "OK");
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
                await _authService.SetAuthHeaderAsync();
                var response = await _httpClient.DeleteAsync($"api/restaurants/my/narrations/{narration.NarrationId}");
                if (response.IsSuccessStatusCode)
                {
                    // Xóa trực tiếp khỏi danh sách đang hiển thị (giao diện tự mất đi)
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
        _activePlayer?.Dispose();
    }

    public class NarrationDto
    {
        public int NarrationId { get; set; }
        public string LanguageName { get; set; }
        public string TextContent { get; set; }
        public string AudioUrl { get; set; }
    }
}