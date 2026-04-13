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
            var userId = await GetUserIdFromToken();
            await _httpClient.PostAsJsonAsync("api/analytics/narration-play", new
            {
                userId,
                restaurantId,
                tourId,
                narrationId,
                languageCode = languageCode ?? "vi",
                latitude,
                longitude
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AnalyticsService] LogNarrationPlay error: {ex.Message}");
        }
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
