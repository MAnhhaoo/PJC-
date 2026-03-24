using Plugin.Maui.Audio;
using System.Net.Http.Json;
using TourismApp.Services;

namespace TourismApp;

public partial class MyAudiosPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer _activePlayer;

    public MyAudiosPage(HttpClient httpClient, AuthService authService, IAudioManager audioManager)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
        _audioManager = audioManager;
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
            listAudios.ItemsSource = res;
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
            if (string.IsNullOrEmpty(narration.AudioUrl))
            {
                await TextToSpeech.Default.SpeakAsync(narration.TextContent);
            }
            else
            {
                // Dùng HttpClient tải stream từ URL và phát bằng AudioManager
                var audioStream = await _httpClient.GetStreamAsync(narration.AudioUrl);

                // Giải phóng player cũ nếu đang phát
                _activePlayer?.Dispose();

                _activePlayer = _audioManager.CreatePlayer(audioStream);
                _activePlayer.Play();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi phát nhạc", "Không thể phát audio: " + ex.Message, "OK");
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
                if (response.IsSuccessStatusCode) await LoadAudios();
                else await DisplayAlert("Lỗi", "Xóa thất bại", "OK");
            }
            catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
        }
    }
}

public class NarrationDto
{
    public int NarrationId { get; set; }
    public string LanguageName { get; set; }
    public string TextContent { get; set; }
    public string AudioUrl { get; set; }
}