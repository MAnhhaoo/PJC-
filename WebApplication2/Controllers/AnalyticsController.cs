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
            var log = new NarrationPlayLog
            {
                UserId = req.UserId > 0 ? req.UserId : null,
                RestaurantId = req.RestaurantId,
                TourId = req.TourId > 0 ? req.TourId : null,
                NarrationId = req.NarrationId > 0 ? req.NarrationId : null,
                LanguageCode = req.LanguageCode,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                PlayedAt = DateTime.Now
            };
            _context.NarrationPlayLogs.Add(log);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("track-point")]
        public async Task<IActionResult> LogTrackPoint([FromBody] TrackPointRequest req)
        {
            var point = new TourTrackPoint
            {
                UserId = req.UserId > 0 ? req.UserId : null,
                TourId = req.TourId,
                SessionId = req.SessionId ?? "",
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                RecordedAt = DateTime.Now
            };
            _context.TourTrackPoints.Add(point);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ===== ANALYTICS ENDPOINTS (called from Blazor Admin) =====

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int days = 30)
        {
            var since = DateTime.Now.AddDays(-days);

            var totalPlays = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since).CountAsync();

            var uniqueVisitors = await _context.NarrationPlayLogs
                .Where(p => p.PlayedAt >= since && p.UserId != null)
                .Select(p => p.UserId)
                .Distinct()
                .CountAsync();

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
                .Where(t => t.RecordedAt >= since);

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
                    t.UserId
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
    }

    public class TrackPointRequest
    {
        public int UserId { get; set; }
        public int TourId { get; set; }
        public string? SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
