using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TourismApp;

public class QRScannerPage : ContentPage
{
    private readonly CameraBarcodeReaderView _barcodeReader;
    private bool _isNavigating;

    public QRScannerPage()
    {
        Title = "Quét QR";

        _barcodeReader = new CameraBarcodeReaderView
        {
            IsDetecting = true,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.QrCode,
                AutoRotate = true,
                Multiple = false
            }
        };
        _barcodeReader.BarcodesDetected += OnBarcodesDetected;

        var statusLabel = new Label
        {
            Text = "Hướng camera vào mã QR nhà hàng",
            TextColor = Colors.White,
            FontSize = 16,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var overlay = new Frame
        {
            BackgroundColor = Color.FromArgb("#AA000000"),
            CornerRadius = 12,
            Padding = new Thickness(16),
            HasShadow = false,
            Content = statusLabel
        };

        var overlayStack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.End,
            Padding = new Thickness(20),
            Spacing = 10,
            Children = { overlay }
        };

        Content = new Grid
        {
            Children = { _barcodeReader, overlayStack }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isNavigating = false;
        _barcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _barcodeReader.IsDetecting = false;
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isNavigating) return;

        var result = e.Results?.FirstOrDefault();
        if (result == null) return;

        var value = result.Value;
        if (string.IsNullOrEmpty(value)) return;

        const string prefix = "tourismapp://restaurant/";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

        var idStr = value.Substring(prefix.Length);
        if (!int.TryParse(idStr, out var restaurantId)) return;

        _isNavigating = true;
        _barcodeReader.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync($"{nameof(RestaurantDetailPage)}?restaurantId={restaurantId}");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể mở nhà hàng: " + ex.Message, "OK");
                _isNavigating = false;
                _barcodeReader.IsDetecting = true;
            }
        });
    }
}
