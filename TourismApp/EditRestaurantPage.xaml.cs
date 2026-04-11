using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using TourismApp.Services;

namespace TourismApp;

public partial class EditRestaurantPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    private FileResult _selectedPhoto;
    private byte[] _selectedImageBytes;
    private FileResult _selectedAudio;
    private double _latitude;
    private double _longitude;

    private List<LanguageItem> _availableLanguages = new();
    private readonly Dictionary<int, CheckBox> _languageCheckboxes = new();

    public EditRestaurantPage(HttpClient httpClient, AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
    }

    private string BuildImageUrl(string imageValue)
    {
        if (string.IsNullOrEmpty(imageValue)) return null;
        if (imageValue.StartsWith("http"))
        {
            try
            {
                var uri = new Uri(imageValue);
                return new Uri(_httpClient.BaseAddress!, uri.PathAndQuery).ToString();
            }
            catch { return imageValue; }
        }
        return new Uri(_httpClient.BaseAddress!, imageValue).ToString();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLanguages();
        await LoadData();
    }

    private async Task LoadLanguages()
    {
        try
        {
            await _authService.SetAuthHeaderAsync();
            var langs = await _httpClient.GetFromJsonAsync<List<LanguageItem>>("api/restaurants/languages");
            if (langs != null) _availableLanguages = langs;
        }
        catch
        {
            _availableLanguages = new List<LanguageItem>
            {
                new() { LanguageId = 1, Code = "vi", Name = "Tiếng Việt" },
                new() { LanguageId = 2, Code = "en", Name = "English" }
            };
        }

        BuildLanguageCheckboxes();
    }

    private void BuildLanguageCheckboxes()
    {
        languageCheckboxContainer.Children.Clear();
        _languageCheckboxes.Clear();

        foreach (var lang in _availableLanguages)
        {
            var row = new HorizontalStackLayout { Spacing = 8 };

            var cb = new CheckBox { Color = Color.FromArgb("#FF9800") };
            cb.CheckedChanged += (s, e) => UpdateLanguageSummary();
            _languageCheckboxes[lang.LanguageId] = cb;

            var label = new Label
            {
                Text = lang.Name,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 14,
                TextColor = Colors.Black
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => cb.IsChecked = !cb.IsChecked;
            label.GestureRecognizers.Add(tapGesture);

            row.Children.Add(cb);
            row.Children.Add(label);
            languageCheckboxContainer.Children.Add(row);
        }
    }

    private void OnToggleLanguageDropdown(object sender, EventArgs e)
    {
        languageCheckboxContainer.IsVisible = !languageCheckboxContainer.IsVisible;
        lblDropdownArrow.Text = languageCheckboxContainer.IsVisible ? "▲" : "▼";
    }

    private void UpdateLanguageSummary()
    {
        var selected = _languageCheckboxes
            .Where(kv => kv.Value.IsChecked)
            .Select(kv => _availableLanguages.FirstOrDefault(l => l.LanguageId == kv.Key)?.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        if (selected.Count == 0)
        {
            lblSelectedLanguages.Text = "Chọn ngôn ngữ thuyết minh...";
            lblSelectedLanguages.TextColor = Color.FromArgb("#888");
        }
        else
        {
            lblSelectedLanguages.Text = string.Join(", ", selected);
            lblSelectedLanguages.TextColor = Color.FromArgb("#333");
        }
    }

    private async Task LoadData()
    {
        try
        {
            await _authService.SetAuthHeaderAsync();
            var res = await _httpClient.GetFromJsonAsync<JsonElement>("api/restaurants/my");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                txtName.Text = res.GetProperty("name").GetString();
                txtAddress.Text = res.GetProperty("address").GetString();
                txtDescription.Text = res.GetProperty("description").GetString();
                txtPhone.Text = res.GetProperty("phone").GetString();

                string imageValue = res.TryGetProperty("image", out var img) ? img.GetString() : null;
                var fullImageUrl = BuildImageUrl(imageValue);
                if (!string.IsNullOrEmpty(fullImageUrl)) imgPreview.Source = fullImageUrl;

                _latitude = res.GetProperty("latitude").GetDouble();
                _longitude = res.GetProperty("longitude").GetDouble();
                lblLocation.Text = $"Tọa độ: {_latitude:F4}, {_longitude:F4}";
                UpdateLocationPin();

                // If restaurant already has valid coordinates, show as confirmed
                if (IsValidCoordinates(_latitude, _longitude))
                {
                    _locationConfirmed = true;
                    btnConfirmLocation.IsVisible = false;
                    locationConfirmedFrame.IsVisible = true;
                    lblLocationConfirmed.Text = $"✅ Đã xác nhận vị trí: {_latitude:F4}, {_longitude:F4}";
                }

                // Load existing narrations — check the languages that already have narrations
                if (res.TryGetProperty("narrations", out var narrations))
                {
                    string firstText = "";
                    foreach (var n in narrations.EnumerateArray())
                    {
                        int langId = n.GetProperty("languageId").GetInt32();
                        string text = n.TryGetProperty("textContent", out var tc) ? tc.GetString() ?? "" : "";
                        string audioUrl = n.TryGetProperty("audioUrl", out var au) ? au.GetString() ?? "" : "";

                        if (string.IsNullOrEmpty(firstText) && !string.IsNullOrEmpty(text))
                            firstText = text;

                        if (_languageCheckboxes.TryGetValue(langId, out var cb))
                            cb.IsChecked = true;

                        if (!string.IsNullOrEmpty(audioUrl))
                        {
                            lblAudioStatus.Text = "✅ Đã có file audio";
                            lblAudioStatus.TextColor = Colors.Green;
                        }
                    }

                    if (!string.IsNullOrEmpty(firstText))
                        txtNarrationContent.Text = firstText;

                    UpdateLanguageSummary();
                }
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await DisplayAlert("Lỗi tải dữ liệu", ex.Message, "OK"));
        }
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

    // === Location with map ===

    private bool _locationConfirmed;

    private static bool IsValidCoordinates(double lat, double lng)
        => lat is >= -90 and <= 90 && lng is >= -180 and <= 180 && (lat != 0 || lng != 0);

    private void UpdateLocationPin()
    {
        if (!IsValidCoordinates(_latitude, _longitude)) return;

        lblLocation.Text = $"Tọa độ: {_latitude:F4}, {_longitude:F4}";
        locationMap.Pins.Clear();
        var pin = new Pin
        {
            Label = "Vị trí nhà hàng",
            Location = new Location(_latitude, _longitude),
            Type = PinType.Place
        };
        locationMap.Pins.Add(pin);
        locationMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(_latitude, _longitude), Distance.FromKilometers(0.5)));

        // Show confirm button, reset confirmed state
        _locationConfirmed = false;
        btnConfirmLocation.IsVisible = true;
        locationConfirmedFrame.IsVisible = false;
    }

    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        _latitude = e.Location.Latitude;
        _longitude = e.Location.Longitude;
        UpdateLocationPin();
    }

    private void OnConfirmLocationClicked(object sender, EventArgs e)
    {
        _locationConfirmed = true;
        btnConfirmLocation.IsVisible = false;
        locationConfirmedFrame.IsVisible = true;
        lblLocationConfirmed.Text = $"✅ Đã xác nhận vị trí: {_latitude:F4}, {_longitude:F4}";
    }

    private async void OnSearchAddressClicked(object sender, EventArgs e)
    {
        var address = txtAddress.Text;
        if (string.IsNullOrWhiteSpace(address))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập địa chỉ để tìm kiếm.", "OK");
            return;
        }

        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(address);
            var location = locations?.FirstOrDefault();
            if (location != null)
            {
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                UpdateLocationPin();
            }
            else
            {
                await DisplayAlert("Không tìm thấy", "Không tìm thấy vị trí cho địa chỉ này. Hãy thử nhập chi tiết hơn.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tìm kiếm địa chỉ: " + ex.Message, "OK");
        }
    }

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            if (location != null)
            {
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                UpdateLocationPin();
            }
        }
        catch { await DisplayAlert("Lỗi", "Hãy bật GPS", "OK"); }
    }

    // === Save ===

    private async void OnSave(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            await DisplayAlert("Thông báo", "Tên nhà hàng không được để trống!", "OK");
            return;
        }

        // Collect selected languages
        var selectedLangIds = _languageCheckboxes
            .Where(kv => kv.Value.IsChecked)
            .Select(kv => kv.Key)
            .ToList();

        // Show loading overlay if narrations are being saved (translation + TTS takes time)
        if (selectedLangIds.Count > 0 && !string.IsNullOrWhiteSpace(txtNarrationContent.Text))
        {
            loadingOverlay.IsVisible = true;
            var langNames = selectedLangIds
                .Select(id => _availableLanguages.FirstOrDefault(l => l.LanguageId == id)?.Name ?? "")
                .Where(n => !string.IsNullOrEmpty(n));
            lblLoadingStatus.Text = $"Đang dịch {string.Join(", ", langNames)}...";
        }

        try
        {
            await _authService.SetAuthHeaderAsync();
            var content = new MultipartFormDataContent();

            // Basic info
            content.Add(new StringContent(txtName.Text ?? ""), "name");
            content.Add(new StringContent(txtAddress.Text ?? ""), "address");
            content.Add(new StringContent(txtDescription.Text ?? ""), "description");
            content.Add(new StringContent(txtPhone.Text ?? ""), "phone");
            content.Add(new StringContent(_latitude.ToString(CultureInfo.InvariantCulture)), "latitude");
            content.Add(new StringContent(_longitude.ToString(CultureInfo.InvariantCulture)), "longitude");

            // Image
            if (_selectedImageBytes != null)
            {
                var imageContent = new ByteArrayContent(_selectedImageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "imageFile", _selectedPhoto.FileName);
            }

            // Multi-language narrations — same text & audio for all selected languages
            for (int i = 0; i < selectedLangIds.Count; i++)
            {
                content.Add(new StringContent(selectedLangIds[i].ToString()), "languageIds");
                content.Add(new StringContent(txtNarrationContent.Text ?? ""), "textContents");

                // Attach same audio file for first entry only — API will reuse for all
                if (i == 0 && _selectedAudio != null)
                {
                    var audioStream = await _selectedAudio.OpenReadAsync();
                    var audioContent = new StreamContent(audioStream);
                    audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                    content.Add(audioContent, $"audioFile_{i}", _selectedAudio.FileName);
                }
            }

            var response = await _httpClient.PutAsync("api/restaurants/update-my", content);

            if (response.IsSuccessStatusCode)
            {
                var langNames = selectedLangIds
                    .Select(id => _availableLanguages.FirstOrDefault(l => l.LanguageId == id)?.Name ?? "")
                    .Where(n => !string.IsNullOrEmpty(n));
                var langList = string.Join(", ", langNames);
                var msg = selectedLangIds.Count > 0
                    ? $"Đã lưu thông tin nhà hàng và thuyết minh ({langList})."
                    : "Đã lưu thông tin nhà hàng.";
                if (_latitude != 0 || _longitude != 0)
                    msg += $"\nTọa độ: {_latitude:F4}, {_longitude:F4}";
                else
                    msg += "\n⚠️ Chưa có tọa độ GPS.";
                await DisplayAlert("Thành công", msg, "OK");
                await Navigation.PopAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", error, "OK");
            }
        }
        catch (Exception ex) { await DisplayAlert("Lỗi hệ thống", ex.Message, "OK"); }
        finally { loadingOverlay.IsVisible = false; }
    }

    private class LanguageItem
    {
        public int LanguageId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}