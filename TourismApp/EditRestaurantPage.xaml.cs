using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using TourismApp.Services;

namespace TourismApp;

public partial class EditRestaurantPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    private FileResult _selectedPhoto;
    private byte[] _selectedImageBytes;
    private FileResult _selectedAudio; // Lưu trữ file audio đã chọn
    private double _latitude;
    private double _longitude;

    public EditRestaurantPage(HttpClient httpClient, AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
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
            await _authService.SetAuthHeaderAsync();
            var res = await _httpClient.GetFromJsonAsync<JsonElement>("api/restaurants/my");

            MainThread.BeginInvokeOnMainThread(() => {
                txtName.Text = res.GetProperty("name").GetString();
                txtAddress.Text = res.GetProperty("address").GetString();
                txtDescription.Text = res.GetProperty("description").GetString();
                txtPhone.Text = res.GetProperty("phone").GetString();

                string imageUrl = res.TryGetProperty("image", out var img) ? img.GetString() : null;
                if (!string.IsNullOrEmpty(imageUrl)) imgPreview.Source = imageUrl;

                _latitude = res.GetProperty("latitude").GetDouble();
                _longitude = res.GetProperty("longitude").GetDouble();
                lblLocation.Text = $"Tọa độ: {_latitude:F4}, {_longitude:F4}";
            });
        }
        catch (Exception ex) { Console.WriteLine($"Lỗi load dữ liệu: {ex.Message}"); }
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        var result = await MediaPicker.Default.PickPhotoAsync();
        if (result != null)
        {
            _selectedPhoto = result;
            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _selectedImageBytes = ms.ToArray();
            imgPreview.Source = ImageSource.FromStream(() => new MemoryStream(_selectedImageBytes));
        }
    }

    private async void OnSelectAudioClicked(object sender, EventArgs e)
    {
        try
        {
            // Định nghĩa loại file thủ công để tránh lỗi CS0117
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Android, new[] { "audio/mpeg", "audio/mp3" } },
            { DevicePlatform.iOS, new[] { "public.mp3" } },
            { DevicePlatform.WinUI, new[] { ".mp3" } }
        });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn file thuyết minh (MP3)",
                FileTypes = customFileType
            });

            if (result != null)
            {
                _selectedAudio = result;
                lblAudioStatus.Text = $"✅ Đã chọn: {result.FileName}";
                lblAudioStatus.TextColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể chọn file: " + ex.Message, "OK");
        }
    }

    // Thêm hàm này vào trong Class EditRestaurantPage
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        // Khi đổi từ Tiếng Việt sang Anh (hoặc ngược lại), xóa nội dung cũ để nhập mới
        txtNarrationContent.Text = string.Empty;
        _selectedAudio = null;
        lblAudioStatus.Text = "Chưa chọn file audio (Máy sẽ tự đọc văn bản)";
    }

    // Sửa lại hàm OnSave của bạn như sau:
    private async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            await DisplayAlert("Thông báo", "Tên nhà hàng không được để trống!", "OK");
            return;
        }

        try
        {
            await _authService.SetAuthHeaderAsync();
            var content = new MultipartFormDataContent();

            // 1. Dữ liệu cơ bản
            content.Add(new StringContent(txtName.Text ?? ""), "name");
            content.Add(new StringContent(txtAddress.Text ?? ""), "address");
            content.Add(new StringContent(txtDescription.Text ?? ""), "description");
            content.Add(new StringContent(txtPhone.Text ?? ""), "phone");
            content.Add(new StringContent(_latitude.ToString()), "latitude");
            content.Add(new StringContent(_longitude.ToString()), "longitude");

            // 2. Gửi Ảnh
            if (_selectedImageBytes != null)
            {
                var imageContent = new ByteArrayContent(_selectedImageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "imageFile", _selectedPhoto.FileName);
            }

            // 3. Gửi Thuyết minh (Quan trọng: Máy đọc văn bản)
            if (pickerLanguage.SelectedIndex != -1)
            {
                int langId = pickerLanguage.SelectedIndex + 1; // 1: Việt, 2: Anh
                content.Add(new StringContent(langId.ToString()), "languageId");
                content.Add(new StringContent(txtNarrationContent.Text ?? ""), "textContent");

                if (_selectedAudio != null) // Chỉ gửi file nếu bạn có chọn
                {
                    var audioStream = await _selectedAudio.OpenReadAsync();
                    var audioContent = new StreamContent(audioStream);
                    audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                    content.Add(audioContent, "audioFile", _selectedAudio.FileName);
                }
            }

            var response = await _httpClient.PutAsync("api/restaurants/update-my", content);
            // Tìm đoạn này trong hàm OnSave
            if (response.IsSuccessStatusCode)
            {
                bool tiepTuc = await DisplayAlert("Thành công",
                    $"Đã lưu thuyết minh {pickerLanguage.SelectedItem}. Bạn có muốn nhập ngôn ngữ khác không?",
                    "Nhập tiếp ngôn ngữ khác", "Hoàn tất");

                if (tiepTuc)
                {
                    // RESET FORM THUYẾT MINH
                    MainThread.BeginInvokeOnMainThread(() => {
                        pickerLanguage.SelectedIndex = -1; // Reset chọn ngôn ngữ
                        txtNarrationContent.Text = string.Empty; // Xóa text cũ
                        _selectedAudio = null; // Xóa file đã chọn trong bộ nhớ
                        lblAudioStatus.Text = "Chưa chọn file audio (.mp3)";
                        lblAudioStatus.TextColor = Colors.Gray;

                        // Cuộn lên đầu phần thuyết minh để người dùng thấy rõ
                        // Optional: txtNarrationContent.Focus(); 
                    });
                }
                else
                {
                    await Navigation.PopAsync();
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", error, "OK");
            }
        }
        catch (Exception ex) { await DisplayAlert("Lỗi hệ thống", ex.Message, "OK"); }
    }
    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            if (location != null)
            {
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                lblLocation.Text = $"Tọa độ: {_latitude:F4}, {_longitude:F4}";
            }
        }
        catch { await DisplayAlert("Lỗi", "Hãy bật GPS", "OK"); }
    }
}