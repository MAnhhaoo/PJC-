namespace TourismApp;

[QueryProperty(nameof(RestaurantId), "restaurantId")]
[QueryProperty(nameof(RestaurantName), "restaurantName")]
public partial class RestaurantQRPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private int _restaurantId;
    private byte[] _qrBytes;

    public string RestaurantId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _restaurantId = id;
        }
    }

    public string RestaurantName
    {
        set => lblRestaurantName.Text = Uri.UnescapeDataString(value ?? "");
    }

    public RestaurantQRPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_restaurantId <= 0) return;

        lblQRContent.Text = $"{_httpClient.BaseAddress}r/{_restaurantId}";

        try
        {
            var url = $"api/restaurants/{_restaurantId}/qrcode";
            _qrBytes = await _httpClient.GetByteArrayAsync(url);

            imgQRCode.Source = ImageSource.FromStream(() => new MemoryStream(_qrBytes));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể tải mã QR: " + ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_qrBytes == null || _qrBytes.Length == 0)
        {
            await DisplayAlert("Lỗi", "Chưa có mã QR để lưu", "OK");
            return;
        }

        try
        {
#if ANDROID
            var fileName = $"QR_NhaHang_{_restaurantId}.png";
            var contentValues = new Android.Content.ContentValues();
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "image/png");
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Pictures/TourismApp");

            var resolver = Android.App.Application.Context.ContentResolver!;
            var uri = resolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, contentValues);

            if (uri != null)
            {
                using var stream = resolver.OpenOutputStream(uri)!;
                await stream.WriteAsync(_qrBytes);
                await stream.FlushAsync();
                await DisplayAlert("Thành công", "Đã lưu mã QR vào thư mục Pictures/TourismApp", "OK");
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể tạo file trong thư viện ảnh", "OK");
            }
#else
            var fileName = $"QR_NhaHang_{_restaurantId}.png";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, _qrBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Lưu mã QR nhà hàng",
                File = new ShareFile(filePath)
            });
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể lưu: " + ex.Message, "OK");
        }
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        if (_qrBytes == null || _qrBytes.Length == 0)
        {
            await DisplayAlert("Lỗi", "Chưa có mã QR để chia sẻ", "OK");
            return;
        }

        try
        {
            var fileName = $"QR_NhaHang_{_restaurantId}.png";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, _qrBytes);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Chia sẻ mã QR nhà hàng",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể chia sẻ: " + ex.Message, "OK");
        }
    }
}
