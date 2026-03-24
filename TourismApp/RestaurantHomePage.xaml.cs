using System.Net.Http;
using System.Net.Http.Json;

namespace TourismApp;

public partial class RestaurantHomePage : ContentPage
{
    private readonly HttpClient _httpClient;

    public RestaurantHomePage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    private async void OnSubmit(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) ||
            string.IsNullOrWhiteSpace(txtAddress.Text) ||
            string.IsNullOrWhiteSpace(txtPhone.Text) ||
            string.IsNullOrWhiteSpace(txtDescription.Text) ||
            string.IsNullOrWhiteSpace(txtImage.Text))
        {
            await DisplayAlert("Lỗi", "Nhập đầy đủ thông tin", "OK");
            return;
        }

        try
        {
            var data = new
            {
                name = txtName.Text,
                address = txtAddress.Text,
                phone = txtPhone.Text,
                description = txtDescription.Text,   // ✅ thêm
                image = txtImage.Text                // ✅ thêm
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/restaurants",
                data);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Lỗi", error, "OK");
                return;
            }

            await DisplayAlert("OK", "Đăng ký thành công", "OK");

            await Shell.Current.GoToAsync(nameof(RestaurantManagerPage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}