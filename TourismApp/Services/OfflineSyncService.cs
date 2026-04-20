using TourismApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace TourismApp.Services;

public class OfflineSyncService
{
    private readonly RestaurantService _restaurantService;
    private readonly HttpClient _httpClient;
    private PoiDbContext _db;

    public OfflineSyncService(RestaurantService restaurantService, HttpClient httpClient)
    {
        _restaurantService = restaurantService;
        _httpClient = httpClient;
        _db = new PoiDbContext();
        EnsureSchemaUpToDate();
    }

    private const int SchemaVersion = 7;

    private void EnsureSchemaUpToDate()
    {
        var currentVersion = Preferences.Default.Get("poi_db_schema_version", 0);
        if (currentVersion < SchemaVersion)
        {
            // Schema changed — wipe and recreate
            _db.Database.EnsureDeleted();
            _db.Database.EnsureCreated();
            Preferences.Default.Set("poi_db_schema_version", SchemaVersion);
        }
        else
        {
            _db.Database.EnsureCreated();
        }
    }

    private void ResetDb()
    {
        _db.Dispose();
        _db = new PoiDbContext();
    }

    public async Task SyncRestaurantsAsync()
    {
        var list = await _restaurantService.GetRestaurantsAsync();
        if (list == null || list.Count == 0) return;

        ResetDb();

        // Preserve existing LocalAudioPath values before clearing
        var existingPaths = await _db.Narrations.AsNoTracking()
            .Where(n => n.LocalAudioPath != null)
            .ToDictionaryAsync(n => n.NarrationId, n => n.LocalAudioPath!);

        // Clear all tables (order matters for foreign keys)
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Narrations");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Restaurants");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM Languages");

        // Insert languages
        var allLanguages = list
            .SelectMany(r => r.Narrations ?? Enumerable.Empty<Narration>())
            .Where(n => n.Language != null)
            .Select(n => n.Language)
            .GroupBy(l => l.LanguageId)
            .Select(g => g.First())
            .ToList();

        foreach (var lang in allLanguages)
            _db.Languages.Add(new Language { LanguageId = lang.LanguageId, Name = lang.Name, Code = lang.Code });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Insert restaurants (flat, no navigation properties)
        foreach (var r in list)
        {
            _db.Restaurants.Add(new Restaurant
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Address = r.Address,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Description = r.Description,
                Image = r.Image,
                IsPremium = r.IsPremium,
                IsActive = r.IsActive,
                PremiumLevel = r.PremiumLevel,
                IsApproved = r.IsApproved,
                PremiumExpireDate = r.PremiumExpireDate,
                BroadcastRadius = r.BroadcastRadius
            });
        }
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Insert narrations (preserve LocalAudioPath from previous sync)
        // Use SelectMany with parent restaurant to preserve RestaurantId
        var allNarrations = list
            .SelectMany(r => (r.Narrations ?? Enumerable.Empty<Narration>())
                .Select(n => new { Narration = n, r.RestaurantId }))
            .GroupBy(x => x.Narration.NarrationId)
            .Select(g => g.First())
            .ToList();

        foreach (var item in allNarrations)
        {
            var n = item.Narration;
            existingPaths.TryGetValue(n.NarrationId, out var localPath);
            _db.Narrations.Add(new Narration
            {
                NarrationId = n.NarrationId,
                DishId = n.DishId,
                RestaurantId = item.RestaurantId,
                LanguageId = n.LanguageId,
                TextContent = n.TextContent,
                AudioUrl = n.AudioUrl,
                LocalAudioPath = localPath
            });
        }
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    public async Task<List<Restaurant>> GetRestaurantsOfflineAsync()
    {
        ResetDb();
        var restaurants = await _db.Restaurants.AsNoTracking().ToListAsync();
        var narrations = await _db.Narrations.AsNoTracking().ToListAsync();
        var languages = await _db.Languages.AsNoTracking().ToListAsync();

        var langDict = languages.ToDictionary(l => l.LanguageId);
        foreach (var n in narrations)
        {
            if (langDict.TryGetValue(n.LanguageId, out var lang))
                n.Language = lang;
        }

        var narrationsByRestaurant = narrations
            .Where(n => n.RestaurantId != null)
            .GroupBy(n => n.RestaurantId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var r in restaurants)
        {
            r.Narrations = narrationsByRestaurant.TryGetValue(r.RestaurantId, out var nars)
                ? nars
                : new List<Narration>();
        }

        return restaurants;
    }

    public async Task DownloadAudioFilesAsync()
    {
        ResetDb();
        var narrations = await _db.Narrations.ToListAsync();
        foreach (var n in narrations)
        {
            if (!string.IsNullOrEmpty(n.AudioUrl))
            {
                var fileName = Path.GetFileName(n.AudioUrl);
                if (string.IsNullOrEmpty(fileName)) continue;

                var localPath = Path.Combine(FileSystem.AppDataDirectory, "audios", fileName);
                if (!File.Exists(localPath))
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        // Build full URL using the HttpClient base address
                        var audioUri = n.AudioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? n.AudioUrl
                            : $"{_httpClient.BaseAddress?.ToString().TrimEnd('/')}/audios/{fileName}";
                        var data = await _httpClient.GetByteArrayAsync(audioUri);
                        await File.WriteAllBytesAsync(localPath, data);
                    }
                    catch
                    {
                        continue;
                    }
                }
                n.LocalAudioPath = localPath;
            }
        }
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    public async Task<Restaurant?> GetRestaurantByIdOfflineAsync(int restaurantId)
    {
        ResetDb();
        var restaurant = await _db.Restaurants.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RestaurantId == restaurantId);
        if (restaurant == null) return null;

        var narrations = await _db.Narrations.AsNoTracking()
            .Where(n => n.RestaurantId == restaurantId)
            .ToListAsync();
        var languages = await _db.Languages.AsNoTracking().ToListAsync();
        var langDict = languages.ToDictionary(l => l.LanguageId);

        foreach (var n in narrations)
        {
            if (langDict.TryGetValue(n.LanguageId, out var lang))
                n.Language = lang;
        }

        restaurant.Narrations = narrations;
        return restaurant;
    }

    public async Task SyncToursAsync()
    {
        try
        {
            var tours = await _httpClient.GetFromJsonAsync<List<Tour>>("api/tours");
            if (tours == null || tours.Count == 0) return;

            ResetDb();

            await _db.Database.ExecuteSqlRawAsync("DELETE FROM TourPOIs");
            await _db.Database.ExecuteSqlRawAsync("DELETE FROM Tours");

            foreach (var t in tours)
            {
                _db.Tours.Add(new Tour
                {
                    TourId = t.TourId,
                    Name = t.Name,
                    Description = t.Description,
                    Image = t.Image,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    Price = t.Price
                });
            }
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            foreach (var t in tours)
            {
                foreach (var poi in t.POIs)
                {
                    _db.TourPOIs.Add(new TourPOI
                    {
                        TourPOIId = poi.TourPOIId,
                        TourId = t.TourId,
                        RestaurantId = poi.RestaurantId,
                        RestaurantName = poi.RestaurantName,
                        RestaurantAddress = poi.RestaurantAddress,
                        RestaurantImage = poi.RestaurantImage,
                        Latitude = poi.Latitude,
                        Longitude = poi.Longitude,
                        OrderIndex = poi.OrderIndex
                    });

                    // Also sync narrations from tour POIs (they may include narrations not in GetRestaurants)
                    if (poi.Narrations != null)
                    {
                        foreach (var n in poi.Narrations)
                        {
                            var exists = await _db.Narrations.AsNoTracking()
                                .AnyAsync(x => x.NarrationId == n.NarrationId);
                            if (!exists)
                            {
                                _db.Narrations.Add(new Narration
                                {
                                    NarrationId = n.NarrationId,
                                    RestaurantId = poi.RestaurantId,
                                    LanguageId = n.LanguageId,
                                    TextContent = n.TextContent,
                                    AudioUrl = n.AudioUrl
                                });
                            }

                            // Sync language if needed
                            if (n.Language != null)
                            {
                                var langExists = await _db.Languages.AsNoTracking()
                                    .AnyAsync(l => l.LanguageId == n.Language.LanguageId);
                                if (!langExists)
                                {
                                    _db.Languages.Add(new Language
                                    {
                                        LanguageId = n.Language.LanguageId,
                                        Name = n.Language.Name,
                                        Code = n.Language.Code
                                    });
                                }
                            }
                        }
                    }
                }
            }
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine("SyncToursAsync error: " + ex.Message);
        }
    }

    public async Task<List<Tour>> GetToursOfflineAsync()
    {
        ResetDb();
        var tours = await _db.Tours.AsNoTracking().ToListAsync();
        var tourPois = await _db.TourPOIs.AsNoTracking().ToListAsync();

        var poisByTour = tourPois.GroupBy(tp => tp.TourId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.OrderIndex).ToList());

        foreach (var t in tours)
        {
            t.POIs = poisByTour.TryGetValue(t.TourId, out var pois)
                ? pois
                : new List<TourPOI>();
        }

        return tours;
    }

    public async Task<Tour?> GetTourByIdOfflineAsync(int tourId)
    {
        ResetDb();
        var tour = await _db.Tours.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TourId == tourId);
        if (tour == null) return null;

        var pois = await _db.TourPOIs.AsNoTracking()
            .Where(tp => tp.TourId == tourId)
            .OrderBy(tp => tp.OrderIndex)
            .ToListAsync();

        // Load narrations for each POI's restaurant
        var narrations = await _db.Narrations.AsNoTracking().ToListAsync();
        var languages = await _db.Languages.AsNoTracking().ToListAsync();
        var langDict = languages.ToDictionary(l => l.LanguageId);

        foreach (var n in narrations)
        {
            if (langDict.TryGetValue(n.LanguageId, out var lang))
                n.Language = lang;
        }

        var narrationsByRestaurant = narrations
            .Where(n => n.RestaurantId != null)
            .GroupBy(n => n.RestaurantId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var poi in pois)
        {
            poi.Narrations = narrationsByRestaurant.TryGetValue(poi.RestaurantId, out var nars)
                ? nars
                : new List<Narration>();
        }

        // Resolve image URLs
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
        foreach (var poi in pois)
        {
            if (!string.IsNullOrEmpty(poi.RestaurantImage) && !poi.RestaurantImage.StartsWith("http"))
                poi.RestaurantImage = $"{baseUrl}/{poi.RestaurantImage.TrimStart('/')}";
        }

        tour.POIs = pois;
        return tour;
    }
}
