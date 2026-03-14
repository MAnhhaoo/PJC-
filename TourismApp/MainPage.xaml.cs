using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System.Net.Http;


namespace TourismApp
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public MainPage(HttpClient httpClient)
        {
            InitializeComponent();
            _httpClient = httpClient;

            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/payments/admin/monthly-revenue");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("API OK", data, "OK");
                }
                else
                {
                    await DisplayAlert("Error", response.StatusCode.ToString(), "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Exception", ex.Message, "OK");
            }
        }
    }
}
