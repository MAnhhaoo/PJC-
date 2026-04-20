using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

[ApiController]
public class GuestViewController : ControllerBase
{
    private readonly AppDbContext _context;

    public GuestViewController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /r/{id} — Guest restaurant web page (no auth)
    [HttpGet("/r/{id}")]
    public async Task<IActionResult> ViewRestaurant(int id)
    {
        var restaurant = await _context.Restaurants
            .Where(r => r.RestaurantId == id)
            .Select(r => new
            {
                r.RestaurantId,
                r.Name,
                r.Address,
                r.Description,
                r.Phone,
                r.Image,
                r.Latitude,
                r.Longitude,
                r.IsActive
            })
            .FirstOrDefaultAsync();

        if (restaurant == null)
            return NotFound("Nhà hàng không tồn tại");

        // Track guest visit
        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault()
                    ?? Request.Query["did"].FirstOrDefault()
                    ?? $"web-{Request.HttpContext.Connection.RemoteIpAddress}";

        var session = await _context.GuestSessions
            .FirstOrDefaultAsync(g => g.DeviceId == deviceId);

        if (session == null)
        {
            session = new GuestSession
            {
                DeviceId = deviceId,
                LastActiveAt = DateTime.UtcNow
            };
            _context.GuestSessions.Add(session);
        }
        else
        {
            session.LastActiveAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Build base URL for images
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var imageUrl = !string.IsNullOrEmpty(restaurant.Image)
            ? (restaurant.Image.StartsWith("http") ? restaurant.Image : $"{baseUrl}{restaurant.Image}")
            : "";

        var deepLink = $"tourismapp://restaurant/{id}";
        var mapUrl = $"https://www.google.com/maps?q={restaurant.Latitude},{restaurant.Longitude}";

        var html = $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{System.Net.WebUtility.HtmlEncode(restaurant.Name ?? "Nhà hàng")}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; color: #333; }}
        .header {{ background: linear-gradient(135deg, #4CAF50, #2E7D32); color: white; padding: 20px; text-align: center; }}
        .header h1 {{ font-size: 22px; margin-bottom: 4px; }}
        .header p {{ font-size: 13px; opacity: 0.9; }}
        .card {{ background: white; margin: 16px; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .card img {{ width: 100%; height: 200px; object-fit: cover; }}
        .card-body {{ padding: 16px; }}
        .card-body h2 {{ font-size: 20px; margin-bottom: 8px; color: #2E7D32; }}
        .info-row {{ display: flex; align-items: flex-start; margin: 10px 0; font-size: 14px; }}
        .info-row .icon {{ font-size: 18px; margin-right: 10px; min-width: 24px; text-align: center; }}
        .info-row .text {{ flex: 1; line-height: 1.4; }}
        .btn {{ display: block; text-align: center; padding: 14px; margin: 12px 16px; border-radius: 10px; text-decoration: none; font-weight: bold; font-size: 15px; }}
        .btn-app {{ background: #4CAF50; color: white; }}
        .btn-map {{ background: #2196F3; color: white; }}
        .btn-app:active {{ background: #388E3C; }}
        .btn-map:active {{ background: #1976D2; }}
        .guest-badge {{ background: #FFF3E0; border: 1px solid #FFE0B2; border-radius: 8px; padding: 10px 16px; margin: 16px; text-align: center; font-size: 13px; color: #E65100; }}
        .footer {{ text-align: center; padding: 20px; color: #999; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🍽️ Tourism App</h1>
        <p>Khám phá ẩm thực địa phương</p>
    </div>

    <div class=""guest-badge"">
        👋 Bạn đang xem với tư cách <b>khách vãng lai</b>. Tải app để có trải nghiệm tốt hơn!
    </div>

    <div class=""card"">
        {(string.IsNullOrEmpty(imageUrl) ? "" : $@"<img src=""{System.Net.WebUtility.HtmlEncode(imageUrl)}"" alt=""{System.Net.WebUtility.HtmlEncode(restaurant.Name)}"" onerror=""this.style.display='none'"" />")}
        <div class=""card-body"">
            <h2>{System.Net.WebUtility.HtmlEncode(restaurant.Name ?? "Nhà hàng")}</h2>

            {(string.IsNullOrEmpty(restaurant.Address) ? "" : $@"<div class=""info-row""><span class=""icon"">📍</span><span class=""text"">{System.Net.WebUtility.HtmlEncode(restaurant.Address)}</span></div>")}

            {(string.IsNullOrEmpty(restaurant.Phone) ? "" : $@"<div class=""info-row""><span class=""icon"">📞</span><span class=""text""><a href=""tel:{System.Net.WebUtility.HtmlEncode(restaurant.Phone)}"">{System.Net.WebUtility.HtmlEncode(restaurant.Phone)}</a></span></div>")}

            {(string.IsNullOrEmpty(restaurant.Description) ? "" : $@"<div class=""info-row""><span class=""icon"">📝</span><span class=""text"">{System.Net.WebUtility.HtmlEncode(restaurant.Description)}</span></div>")}
        </div>
    </div>

    <a class=""btn btn-app"" href=""{deepLink}"">📱 Mở trong ứng dụng Tourism App</a>
    <a class=""btn btn-map"" href=""{mapUrl}"" target=""_blank"">🗺️ Xem trên Google Maps</a>

    <div class=""footer"">
        <p>Quét mã QR bằng ứng dụng Tourism App để có trải nghiệm đầy đủ</p>
        <p>© Tourism App {DateTime.Now.Year}</p>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }
}
