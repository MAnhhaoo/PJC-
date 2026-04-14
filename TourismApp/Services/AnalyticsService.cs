using System.Net.Http.Json;
using System.Text.Json;

namespace TourismApp.Services;

public class AnalyticsService
{
    private readonly HttpClient _httpClient;

    public AnalyticsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LogNarrationPlayAsync(int restaurantId, int? tourId, int narrationId, string languageCode, double latitude, double longitude)
    {
        try
        {
            var (userId, guestLabel) = await GetIdentityAsync();
            var response = await _httpClient.PostAsJsonAsync("api/analytics/narration-play", new
            {
                userId,
                restaurantId,
                tourId,
                narrationId,
                languageCode = languageCode ?? "vi",
                latitude,
                longitude,
                guestLabel
            });
            System.Diagnostics.Debug.WriteLine($"[AnalyticsService] LogNarrationPlay: status={response.StatusCode}, restaurantId={restaurantId}, userId={userId}, guest={guestLabel}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[AnalyticsService] LogNarrationPlay server error: {body}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnalyticsService] LogNarrationPlay error: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns (userId, guestLabel). When anonymous mode is ON, userId=0 and guestLabel="Guest_XXXX".
    /// </summary>
    public static async Task<(int userId, string? guestLabel)> GetIdentityAsync()
    {
        bool anonymous = Preferences.Default.Get("anonymous_mode", false);
        if (anonymous)
        {
            var label = GetOrCreateGuestLabel();
            return (0, label);
        }

        var id = await GetUserIdFromToken();
        return (id, null);
    }

    private static string GetOrCreateGuestLabel()
    {
        var existing = Preferences.Default.Get("guest_label", "");
        if (!string.IsNullOrEmpty(existing)) return existing;

        var label = "Guest_" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        Preferences.Default.Set("guest_label", label);
        return label;
    }

    private static async Task<int> GetUserIdFromToken()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token)) return 0;

            var parts = token.Split('.');
            if (parts.Length < 2) return 0;

            var payload = parts[1];
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var claimName in new[] { "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "sub", "nameid" })
            {
                if (root.TryGetProperty(claimName, out var val))
                {
                    var s = val.ToString();
                    if (int.TryParse(s, out var id)) return id;
                }
            }
            return 0;
        }
        catch { return 0; }
    }
}
