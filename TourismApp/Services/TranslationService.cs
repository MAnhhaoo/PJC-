using System.Collections.Concurrent;
using System.Text.Json;

namespace TourismApp.Services;

public class TranslationService
{
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private const string CachePreferenceKey = "translation_cache";

    public TranslationService()
    {
        LoadCacheFromPreferences();
    }

    public async Task<string> TranslateAsync(string text, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text) || targetLang == "vi")
            return text;

        var cacheKey = $"{targetLang}:{text}";
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair=vi|{targetLang}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return text;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("responseData", out var data)
                && data.TryGetProperty("translatedText", out var t))
            {
                var translated = t.GetString() ?? text;
                _cache[cacheKey] = translated;
                SaveCacheToPreferences();
                return translated;
            }
        }
        catch { }

        return text;
    }

    public async Task TranslateRestaurantAsync(Models.Restaurant r, string targetLang)
    {
        if (targetLang == "vi" || r == null) return;

        var tasks = new List<Task>();

        tasks.Add(Task.Run(async () => r.Name = await TranslateAsync(r.Name, targetLang)));
        tasks.Add(Task.Run(async () => r.Address = await TranslateAsync(r.Address, targetLang)));
        tasks.Add(Task.Run(async () => r.Description = await TranslateAsync(r.Description, targetLang)));

        await Task.WhenAll(tasks);
    }

    public async Task TranslateRestaurantsAsync(List<Models.Restaurant> restaurants, string targetLang)
    {
        if (targetLang == "vi" || restaurants == null || restaurants.Count == 0) return;

        foreach (var r in restaurants)
            await TranslateRestaurantAsync(r, targetLang);
    }

    private void LoadCacheFromPreferences()
    {
        try
        {
            var json = Preferences.Default.Get(CachePreferenceKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _cache[kv.Key] = kv.Value;
                }
            }
        }
        catch { }
    }

    private void SaveCacheToPreferences()
    {
        try
        {
            // Limit cache size to avoid Preferences overflow
            var toSave = _cache.Take(500).ToDictionary(kv => kv.Key, kv => kv.Value);
            var json = JsonSerializer.Serialize(toSave);
            Preferences.Default.Set(CachePreferenceKey, json);
        }
        catch { }
    }
}
