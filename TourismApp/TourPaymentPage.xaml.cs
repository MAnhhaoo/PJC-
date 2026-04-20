using System.Net.Http.Json;
using System.Text.Json;

namespace TourismApp;

[QueryProperty(nameof(TourIdStr), "tourId")]
[QueryProperty(nameof(TourNameStr), "tourName")]
[QueryProperty(nameof(PriceStr), "price")]
public partial class TourPaymentPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private int _tourId;
    private decimal _price;
    private int _paymentId;
    private string _referenceCode = "";
    private CancellationTokenSource? _pollCts;
    private bool _paymentConfirmed;
    private string _lastQrUrl = "";

    // === Thông tin ngân hàng nhận tiền ===
    private const string BankId = "970423";              // Mã BIN ngân hàng (TPBank)
    private const string AccountNo = "00003906130";
    private const string AccountName = "MAC ANH HAO";    // Tên chủ tài khoản
    private const string BankDisplayName = "TPBank";

    public string TourIdStr
    {
        set { if (int.TryParse(value, out var id)) _tourId = id; }
    }

    public string TourNameStr
    {
        set { _tourName = Uri.UnescapeDataString(value ?? ""); }
    }
    private string _tourName = "";

    public string PriceStr
    {
        set { if (decimal.TryParse(value, out var p)) _price = p; }
    }

    public TourPaymentPage(HttpClient httpClient)
    {
        InitializeComponent();
        _httpClient = httpClient;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        lblTourName.Text = _tourName;
        lblPrice.Text = $"{_price:N0}đ";

        // Show bank info
        lblBankName.Text = $"Ngân hàng: {BankDisplayName}";
        lblAccountNo.Text = $"STK: {AccountNo}";
        lblAccountName.Text = $"Chủ TK: {AccountName}";

        await CreatePaymentAndShowQR();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        _pollCts?.Cancel();

        // Cancel pending payment if user exits without paying
        if (!_paymentConfirmed && _tourId > 0)
        {
            try
            {
                var token = await SecureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, $"api/payments/cancel-tour/{_tourId}");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    await _httpClient.SendAsync(request);
                }
            }
            catch { }
        }
    }

    private async Task CreatePaymentAndShowQR()
    {
        var token = await SecureStorage.GetAsync("auth_token");
        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert("Lỗi", "Bạn cần đăng nhập để thanh toán", "OK");
            return;
        }

        try
        {
            qrLoading.IsVisible = true;
            qrLoading.IsRunning = true;
            btnRetryQR.IsVisible = false;
            lblStatus.Text = "⏳ Đang tạo yêu cầu thanh toán...";
            lblStatus.TextColor = Color.FromArgb("#888");

            var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/purchase-tour");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { tourId = _tourId, paymentMethod = "QR" });

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                qrLoading.IsVisible = false;
                SecureStorage.Remove("auth_token");
                await DisplayAlert("Phiên hết hạn", "Phiên đăng nhập đã hết hạn.\nVui lòng đăng nhập lại.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                lblStatus.Text = $"❌ Lỗi ({(int)response.StatusCode}): {json}";
                lblStatus.TextColor = Color.FromArgb("#D32F2F");
                qrLoading.IsVisible = false;
                btnRetryQR.IsVisible = true;
                return;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _paymentId = root.GetProperty("paymentId").GetInt32();
            _referenceCode = root.TryGetProperty("referenceCode", out var refProp)
                ? refProp.GetString() ?? $"TPA{_paymentId}"
                : $"TPA{_paymentId}";
            var amount = root.TryGetProperty("amount", out var amtProp)
                ? amtProp.GetDecimal() : _price;

            // Generate VietQR image URL (real bank-scannable QR)
            _lastQrUrl = $"https://img.vietqr.io/image/{BankId}-{AccountNo}-compact2.png"
                + $"?amount={amount:0}"
                + $"&addInfo={Uri.EscapeDataString(_referenceCode)}"
                + $"&accountName={Uri.EscapeDataString(AccountName)}";

            // Download QR image bytes directly (more reliable on Android)
            await LoadQRImageAsync(_lastQrUrl);

            // Show transfer details
            lblAmount.Text = $"Số tiền: {amount:N0}đ";
            lblTransferContent.Text = $"Nội dung CK: {_referenceCode}";
            lblStatus.Text = "";

            // Show polling frame and start polling
            framePolling.IsVisible = true;
            StartPolling(token);
        }
        catch (HttpRequestException ex)
        {
            lblStatus.Text = $"❌ Không kết nối được server:\n{_httpClient.BaseAddress}\n{ex.Message}";
            lblStatus.TextColor = Color.FromArgb("#D32F2F");
            qrLoading.IsVisible = false;
            btnRetryQR.IsVisible = true;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"❌ Lỗi: {ex.Message}";
            lblStatus.TextColor = Color.FromArgb("#D32F2F");
            qrLoading.IsVisible = false;
            btnRetryQR.IsVisible = true;
        }
    }

    private async Task LoadQRImageAsync(string qrUrl)
    {
        try
        {
            lblStatus.Text = "⏳ Đang tải mã QR...";
            using var qrClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var imageBytes = await qrClient.GetByteArrayAsync(qrUrl);
            imgQRCode.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
            qrLoading.IsVisible = false;
            btnRetryQR.IsVisible = false;
        }
        catch (Exception ex)
        {
            qrLoading.IsVisible = false;
            btnRetryQR.IsVisible = true;
            lblStatus.Text = $"❌ Không tải được QR: {ex.Message}\nNhấn 'Tải lại' để thử lại.";
            lblStatus.TextColor = Color.FromArgb("#D32F2F");
        }
    }

    private async void OnRetryQRClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastQrUrl))
        {
            await LoadQRImageAsync(_lastQrUrl);
        }
        else
        {
            await CreatePaymentAndShowQR();
        }
    }

    private void StartPolling(string token)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        var ct = _pollCts.Token;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(3000, ct);
                if (ct.IsCancellationRequested) break;

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"api/payments/check-tour/{_tourId}");
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    var response = await _httpClient.SendAsync(request, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(ct);
                        using var doc = JsonDocument.Parse(json);
                        var isPurchased = doc.RootElement.GetProperty("isPurchased").GetBoolean();

                        if (isPurchased)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                _paymentConfirmed = true;
                                _pollCts?.Cancel();

                                // Show success
                                framePolling.IsVisible = false;
                                frameSuccess.IsVisible = true;

                                await DisplayAlert("🎉 Thành công!",
                                    $"Thanh toán tour \"{_tourName}\" thành công!\nTour đã được mở khóa.",
                                    "Bắt đầu Tour");

                                await Shell.Current.GoToAsync("..");
                            });
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* continue polling */ }
            }
        }, ct);
    }
}
