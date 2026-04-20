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

        int restaurantId = -1;
        bool isHomeLink = false;

        // Support tourismapp://home — navigate to home page
        if (value.StartsWith("tourismapp://home", StringComparison.OrdinalIgnoreCase))
        {
            isHomeLink = true;
        }
        // Support old format: tourismapp://restaurant/{id}
        else if (value.StartsWith("tourismapp://restaurant/", StringComparison.OrdinalIgnoreCase))
        {
            var idStr = value.Substring("tourismapp://restaurant/".Length);
            int.TryParse(idStr, out restaurantId);
        }
        // Support new HTTP format: http(s)://.../r/{id}
        else if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                 && uri.Segments.Length >= 2
                 && uri.Segments[uri.Segments.Length - 2].TrimEnd('/') == "r")
        {
            int.TryParse(uri.Segments[uri.Segments.Length - 1].TrimEnd('/'), out restaurantId);
        }

        if (!isHomeLink && restaurantId <= 0) return;

        _isNavigating = true;
        _barcodeReader.IsDetecting = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (isHomeLink)
                {
                    await Shell.Current.GoToAsync("//CustomerHomePage");
                }
                else
                {
                    await Shell.Current.GoToAsync($"{nameof(RestaurantDetailPage)}?restaurantId={restaurantId}");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể mở: " + ex.Message, "OK");
                _isNavigating = false;
                _barcodeReader.IsDetecting = true;
            }
        });
    }
}
