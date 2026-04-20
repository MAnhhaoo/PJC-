using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApplication2.Data;

namespace WebApplication2.Services;

public class SepaySettings
{
    public string ApiKey { get; set; } = "";
    public string AccountNumber { get; set; } = "00003906130";
    public int PollingIntervalSeconds { get; set; } = 10;
}

/// <summary>
/// Background service that polls SePay API for new bank transactions
/// and auto-confirms matching pending payments.
/// </summary>
public class BankTransactionPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BankTransactionPollingService> _logger;
    private readonly SepaySettings _settings;
    private readonly HttpClient _httpClient;

    public BankTransactionPollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<BankTransactionPollingService> logger,
        IOptions<SepaySettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://my.sepay.vn/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogWarning("SePay API key not configured. Bank transaction polling is disabled. " +
                "Add SePay:ApiKey to appsettings.json to enable auto-confirmation.");
            return;
        }

        _logger.LogInformation("Bank transaction polling started (every {Interval}s)", _settings.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckTransactionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling bank transactions");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task CheckTransactionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get all pending payments
        var pendingPayments = await context.Payments
            .Include(p => p.Restaurant)
            .Include(p => p.User)
            .Where(p => p.Status == "Pending")
            .ToListAsync(ct);

        if (!pendingPayments.Any()) return;

        // Call SePay API to get recent transactions
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var url = $"userapi/transactions/list?" +
                  $"account_number={_settings.AccountNumber}" +
                  $"&transaction_date_min={today}" +
                  $"&limit=50";

        var response = await _httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("SePay API returned {Status}: {Body}", response.StatusCode, errBody);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("SePay response: {Json}", json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("transactions", out var transactions))
        {
            _logger.LogWarning("SePay response has no 'transactions' property. Keys: {Keys}",
                string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
            return;
        }

        _logger.LogInformation("SePay: {Count} transactions found, {Pending} pending payments",
            transactions.GetArrayLength(), pendingPayments.Count);

        foreach (var tx in transactions.EnumerateArray())
        {
            var content = tx.TryGetProperty("transaction_content", out var contentProp)
                ? contentProp.GetString() ?? ""
                : "";

            var amount = tx.TryGetProperty("amount_in", out var amountProp)
                ? (amountProp.ValueKind == JsonValueKind.Number
                    ? amountProp.GetDecimal()
                    : decimal.TryParse(amountProp.GetString(), out var parsed) ? parsed : 0)
                : 0;

            if (amount <= 0 || string.IsNullOrEmpty(content))
                continue;

            // Match reference code pattern TPA{paymentId}
            var match = System.Text.RegularExpressions.Regex.Match(
                content, @"TPA(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!match.Success) continue;

            var paymentId = int.Parse(match.Groups[1].Value);
            var payment = pendingPayments.FirstOrDefault(p => p.PaymentId == paymentId);

            if (payment == null || payment.Status != "Pending") continue;

            // Verify amount
            if (amount < payment.Amount)
            {
                _logger.LogWarning("Payment {Id}: amount insufficient ({Received} < {Expected})",
                    paymentId, amount, payment.Amount);
                continue;
            }

            // Confirm payment
            payment.Status = "Success";
            payment.PaymentDate = DateTime.Now;

            if (payment.PaymentType == "RestaurantRegistration" && payment.Restaurant != null)
            {
                payment.Restaurant.IsApproved = true;
            }
            else if (payment.PaymentType == "RestaurantRegistrationPremium" && payment.Restaurant != null)
            {
                payment.Restaurant.IsApproved = true;
                payment.Restaurant.IsPremium = true;
                payment.Restaurant.PremiumExpireDate = payment.ExpireDate ?? DateTime.Now.AddYears(1);
            }
            else if (payment.PaymentType == "RestaurantPremium" && payment.Restaurant != null)
            {
                payment.Restaurant.IsPremium = true;
                payment.Restaurant.PremiumExpireDate = payment.ExpireDate ?? DateTime.Now.AddMonths(1);
            }
            else if (payment.PaymentType == "UserUpgrade" && payment.User != null)
            {
                payment.User.UserLevel = 1;
            }

            _logger.LogInformation("✅ Auto-confirmed payment {Id} ({Type}, {Amount}đ)",
                paymentId, payment.PaymentType, amount);
        }

        await context.SaveChangesAsync(ct);
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}
