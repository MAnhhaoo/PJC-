using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        // ===== LOGGING ENDPOINTS (called from MAUI app) =====

        [HttpPost("narration-play")]
        public async Task<IActionResult> LogNarrationPlay([FromBody] NarrationPlayRequest req)
        {
            try
            {
                var log = new NarrationPlayLog
                {
                    UserId = req.UserId > 0 ? req.UserId : null,
                    RestaurantId = req.RestaurantId,
                    TourId = req.TourId > 0 ? req.TourId : null,
                    NarrationId = req.NarrationId > 0 ? req.NarrationId : null,
                    LanguageCode = req.LanguageCode,
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    GuestLabel = req.GuestLabel,
                    PlayedAt = DateTime.Now
                };
                _context.NarrationPlayLogs.Add(log);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analytics] LogNarrationPlay ERROR: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpPost("track-point")]
        public async Task<IActionResult> LogTrackPoint([FromBody] TrackPointRequest req)
        {
            try
            {
                var point = new TourTrackPoint
                {
                    UserId = req.UserId > 0 ? req.UserId : null,
                    TourId = req.TourId,
                    SessionId = req.SessionId ?? "",
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    GuestLabel = req.GuestLabel,
                    RecordedAt = DateTime.Now
                };
                _context.TourTrackPoints.Add(point);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analytics] LogTrackPoint ERROR: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ===== ANALYTICS ENDPOINTS (called from Blazor Admin) =====

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int days = 30)
        {
            var since = DateTime.Now.AddDays(-days);

            var totalPlays = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since).CountAsync();

            var uniqueRegistered = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since && p.UserId != null)
                .Select(p => p.UserId)
                .Distinct()
                .CountAsync();

            var uniqueGuests = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since && p.UserId == null && p.GuestLabel != null)
                .Select(p => p.GuestLabel)
                .Distinct()
                .CountAsync();

            var uniqueVisitors = uniqueRegistered + uniqueGuests;

            var totalSessions = await _context.TourTrackPoints
                .Where(t => t.RecordedAt >= since)
                .Select(t => t.SessionId)
                .Distinct()
                .CountAsync();

            var totalPOIsVisited = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since)
                .Select(p => p.RestaurantId)
                .Distinct()
                .CountAsync();

            var dailyPlays = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since)
                .GroupBy(p => p.PlayedAt.Date)
                .Select(g => new { date = g.Key, count = g.Count() })
                .OrderBy(x => x.date)
                .ToListAsync();

            return Ok(new
            {
                totalPlays,
                uniqueVisitors,
                totalSessions,
                totalPOIsVisited,
                dailyPlays = dailyPlays.Select(d => new { date = d.date.ToString("dd/MM"), count = d.count })
            });
        }

        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmapData([FromQuery] int days = 30)
        {
            var since = DateTime.Now.AddDays(-days);

            // Aggregate from NarrationPlayLogs
            var playData = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since)
                .GroupBy(p => p.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Aggregate from LocationHistory
            var visitData = await _context.LocationHistories
                .Where(l => l.VisitedAt >= since)
                .GroupBy(l => l.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Merge both sources
            var merged = playData.Concat(visitData)
                .GroupBy(x => x.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Sum(x => x.Count) })
                .ToList();

            // Get restaurant coordinates
            var restaurantIds = merged.Select(m => m.RestaurantId).ToList();
            var restaurants = await _context.Restaurants
                .Where(r => restaurantIds.Contains(r.RestaurantId))
                .Select(r => new { r.RestaurantId, r.Name, r.Latitude, r.Longitude })
                .ToListAsync();

            var result = merged.Select(m =>
            {
                var r = restaurants.FirstOrDefault(x => x.RestaurantId == m.RestaurantId);
                return new
                {
                    lat = r?.Latitude ?? 0,
                    lng = r?.Longitude ?? 0,
                    name = r?.Name ?? "",
                    count = m.Count
                };
            })
            .Where(x => Math.Abs(x.lat) > 0.0001 || Math.Abs(x.lng) > 0.0001)
            .ToList();

            return Ok(result);
        }

        [HttpGet("top-pois")]
        public async Task<IActionResult> GetTopPOIs([FromQuery] int days = 30, [FromQuery] int top = 10)
        {
            var since = DateTime.Now.AddDays(-days);

            var data = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since)
                .GroupBy(p => p.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, PlayCount = g.Count() })
                .OrderByDescending(x => x.PlayCount)
                .Take(top)
                .ToListAsync();

            var restaurantIds = data.Select(d => d.RestaurantId).ToList();
            var restaurants = await _context.Restaurants
                .Where(r => restaurantIds.Contains(r.RestaurantId))
                .Select(r => new { r.RestaurantId, r.Name, r.Address, r.Latitude, r.Longitude })
                .ToListAsync();

            var result = data.Select(d =>
            {
                var r = restaurants.FirstOrDefault(x => x.RestaurantId == d.RestaurantId);
                return new
                {
                    restaurantId = d.RestaurantId,
                    name = r?.Name ?? "Unknown",
                    address = r?.Address ?? "",
                    lat = r?.Latitude ?? 0,
                    lng = r?.Longitude ?? 0,
                    playCount = d.PlayCount
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("journeys")]
        public async Task<IActionResult> GetJourneys([FromQuery] int days = 7, [FromQuery] int? tourId = null)
        {
            var since = DateTime.Now.AddDays(-days);

            var query = _context.TourTrackPoints
                .Where(t => t.RecordedAt >= since)
                .Where(t => t.GuestLabel != null); // Chỉ lấy khách ẩn danh

            if (tourId.HasValue && tourId > 0)
                query = query.Where(t => t.TourId == tourId.Value);

            var points = await query
                .OrderBy(t => t.SessionId)
                .ThenBy(t => t.RecordedAt)
                .Select(t => new
                {
                    t.SessionId,
                    t.TourId,
                    t.Latitude,
                    t.Longitude,
                    t.RecordedAt,
                    t.UserId,
                    t.GuestLabel
                })
                .ToListAsync();

            var tourIds = points.Select(p => p.TourId).Distinct().ToList();
            var tours = await _context.Tours
                .Where(t => tourIds.Contains(t.TourId))
                .Select(t => new { t.TourId, t.Name })
                .ToListAsync();

            var sessions = points
                .GroupBy(p => p.SessionId)
                .Select(g => new
                {
                    sessionId = g.Key,
                    tourId = g.First().TourId,
                    tourName = tours.FirstOrDefault(t => t.TourId == g.First().TourId)?.Name ?? "",
                    userId = g.First().UserId,
                    guestLabel = g.First().GuestLabel,
                    startTime = g.Min(x => x.RecordedAt),
                    pointCount = g.Count(),
                    points = g.Select(x => new { lat = x.Latitude, lng = x.Longitude, time = x.RecordedAt }).ToList()
                })
                .OrderByDescending(s => s.startTime)
                .Take(50)
                .ToList();

            return Ok(sessions);
        }

        [HttpGet("tours")]
        public async Task<IActionResult> GetTourList()
        {
            var tours = await _context.Tours
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new { t.TourId, t.Name })
                .ToListAsync();
            return Ok(tours);
        }

        /// <summary>
        /// Seed sample analytics data for testing (NarrationPlayLogs + TourTrackPoints).
        /// Includes both registered users and anonymous guests.
        /// </summary>
        [HttpPost("seed-test-data")]
        public async Task<IActionResult> SeedTestData()
        {
            try
            {
                var rng = new Random();

                // Get existing restaurants (POIs) with coordinates
                var restaurants = await _context.Restaurants
                    .Where(r => r.Latitude != 0 || r.Longitude != 0)
                    .Select(r => new { r.RestaurantId, r.Latitude, r.Longitude })
                    .Take(20)
                    .ToListAsync();

                if (restaurants.Count == 0)
                    return BadRequest("Không có nhà hàng/điểm POI nào trong database.");

                // Get existing tours
                var tours = await _context.Tours
                    .Where(t => t.IsActive)
                    .Select(t => t.TourId)
                    .Take(5)
                    .ToListAsync();

                // Get real user IDs from database
                var userIds = await _context.Users
                    .Select(u => u.UserId)
                    .Take(5)
                    .ToListAsync();

                // Guest labels for anonymous users
                var guestLabels = new[] { "Guest_A1B2C3D4", "Guest_E5F6G7H8", "Guest_I9J0K1L2" };

                var now = DateTime.Now;

                // ===== 1. Seed NarrationPlayLogs =====
                var playLogs = new List<NarrationPlayLog>();
                for (int day = 0; day < 14; day++)
                {
                    int playsPerDay = rng.Next(3, 10);
                    for (int p = 0; p < playsPerDay; p++)
                    {
                        var rest = restaurants[rng.Next(restaurants.Count)];
                        bool isGuest = userIds.Count == 0 || rng.NextDouble() < 0.4;

                        playLogs.Add(new NarrationPlayLog
                        {
                            UserId = isGuest || userIds.Count == 0 ? null : userIds[rng.Next(userIds.Count)],
                            RestaurantId = rest.RestaurantId,
                            TourId = tours.Count > 0 ? tours[rng.Next(tours.Count)] : null,
                            NarrationId = null,
                            LanguageCode = rng.NextDouble() < 0.7 ? "vi" : "en",
                            Latitude = rest.Latitude + (rng.NextDouble() - 0.5) * 0.001,
                            Longitude = rest.Longitude + (rng.NextDouble() - 0.5) * 0.001,
                            GuestLabel = isGuest ? guestLabels[rng.Next(guestLabels.Length)] : null,
                            PlayedAt = now.AddDays(-day).AddHours(rng.Next(8, 20)).AddMinutes(rng.Next(60))
                        });
                    }
                }
                _context.NarrationPlayLogs.AddRange(playLogs);

                // ===== 2. Seed TourTrackPoints (journey data) =====
                var trackPoints = new List<TourTrackPoint>();
                if (tours.Count > 0)
                {
                    for (int s = 0; s < 5; s++)
                    {
                        var sessionId = Guid.NewGuid().ToString("N")[..16];
                        var tourId = tours[rng.Next(tours.Count)];
                        bool isGuest = s < 2;
                        int? userId = isGuest || userIds.Count == 0 ? null : userIds[rng.Next(userIds.Count)];
                        string? guestLabel = isGuest ? guestLabels[Math.Min(s, guestLabels.Length - 1)] : null;

                        var startRest = restaurants[rng.Next(restaurants.Count)];
                        double lat = startRest.Latitude;
                        double lng = startRest.Longitude;
                        var startTime = now.AddDays(-rng.Next(0, 7)).AddHours(rng.Next(8, 18));

                        int pointCount = rng.Next(15, 31);
                        for (int pt = 0; pt < pointCount; pt++)
                        {
                            lat += (rng.NextDouble() - 0.4) * 0.002;
                            lng += (rng.NextDouble() - 0.4) * 0.002;

                            trackPoints.Add(new TourTrackPoint
                            {
                                UserId = userId,
                                TourId = tourId,
                                SessionId = sessionId,
                                Latitude = lat,
                                Longitude = lng,
                                GuestLabel = guestLabel,
                                RecordedAt = startTime.AddSeconds(pt * 30)
                            });
                        }
                    }
                    _context.TourTrackPoints.AddRange(trackPoints);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Đã tạo dữ liệu test thành công!",
                    narrationPlays = playLogs.Count,
                    tourTrackPoints = trackPoints.Count,
                    guestSessions = tours.Count > 0 ? 2 : 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }

    // Request DTOs
    public class NarrationPlayRequest
    {
        public int UserId { get; set; }
        public int RestaurantId { get; set; }
        public int? TourId { get; set; }
        public int? NarrationId { get; set; }
        public string? LanguageCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? GuestLabel { get; set; }
    }

    public class TrackPointRequest
    {
        public int UserId { get; set; }
        public int TourId { get; set; }
        public string? SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? GuestLabel { get; set; }
    }
}
