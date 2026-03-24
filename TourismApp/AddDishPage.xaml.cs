using System.Net.Http.Json;
using TourismApp.Services;

namespace TourismApp;

public partial class AddDishPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public AddDishPage(HttpClient httpClient, AuthService authService)
    {
        InitializeComponent();
        _httpClient = httpClient;
        _authService = authService;
    }

    FileResult selectedPhoto;

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        // Mở thư viện ảnh của điện thoại
        selectedPhoto = await MediaPicker.Default.PickPhotoAsync();

        if (selectedPhoto != null)
        {
            // Hiển thị ảnh vừa chọn lên màn hình để xem trước
            var stream = await selectedPhoto.OpenReadAsync();
            imgPreview.Source = ImageSource.FromStream(() => stream);
        }
    }

    private async void OnAddDish(object sender, EventArgs e)
    {
        if (selectedPhoto == null)
        {
            await DisplayAlert("Lỗi", "Vui lòng chọn ảnh", "OK");
            return;
        }

        await _authService.SetAuthHeaderAsync();

        // Dùng MultipartFormDataContent thay vì PostAsJson
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(txtName.Text), "name");

        var fileStream = await selectedPhoto.OpenReadAsync();
        var fileContent = new StreamContent(fileStream);
        content.Add(fileContent, "image", selectedPhoto.FileName);

        var res = await _httpClient.PostAsync("api/dishes", content);

        if (res.IsSuccessStatusCode)
        {
            await DisplayAlert("Thành công", "Đã thêm món với ảnh từ máy!", "OK");
            await Navigation.PopAsync();
        }
    }
}