using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.DTOs;
using WebApplication2.Models;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public RestaurantsController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(text) || string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
            return text;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

        var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={sourceLang}|{targetLang}";
        var resp = await client.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return text;

        var content = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("responseData", out var data) && data.TryGetProperty("translatedText", out var t))
            {
                return t.GetString() ?? text;
            }
        }
        catch { }

        return text;
    }

    // 🔹 GET: api/restaurants
    [HttpGet]
    public IActionResult GetRestaurants()
    {
        var restaurants = _context.Restaurants
            .Where(r => r.IsApproved && (!r.IsPremium || r.PremiumExpireDate == null || r.PremiumExpireDate > DateTime.Now))
            .Select(r => new RestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Address = r.Address,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Image = r.Image,
                IsPremium = r.IsPremium,
                IsApproved = r.IsApproved,

                // THÊM DÒNG NÀY VÀO ĐỂ GỬI THUYẾT MINH VỀ MOBILE
                Narrations = r.Narrations.Select(n => new NarrationDto
                {
                    NarrationId = n.NarrationId,
                    LanguageId = n.LanguageId,
                    TextContent = n.TextContent,
                    // Trả về đường dẫn tương đối (client tự ghép IP)
                    AudioUrl = string.IsNullOrEmpty(n.AudioUrl) ? "" :
                               n.AudioUrl.Replace("audios/", "").TrimStart('/'),
                    Language = new LanguageDto
                    {
                        LanguageId = n.Language.LanguageId,
                        Code = n.Language.Code,
                        Name = n.Language.Name
                    }
                }).ToList()
            })
            .ToList();

        return Ok(restaurants);
    }

    // 🔹 GET: api/restaurants/languages
    [HttpGet("languages")]
    public IActionResult GetLanguages()
    {
        var languages = _context.Languages
            .Select(l => new { l.LanguageId, l.Code, l.Name })
            .ToList();
        return Ok(languages);
    }

    // 🔹 GET: api/restaurants/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRestaurantById(int id)
    {
        var restaurant = await _context.Restaurants
            .Where(r => r.RestaurantId == id && r.IsApproved)
            .Select(r => new RestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Address = r.Address,
                Description = r.Description,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Image = r.Image,
                IsPremium = r.IsPremium,
                IsActive = r.IsActive,
                IsApproved = r.IsApproved,
                PremiumExpireDate = r.PremiumExpireDate,
                Narrations = r.Narrations.Select(n => new NarrationDto
                {
                    NarrationId = n.NarrationId,
                    LanguageId = n.LanguageId,
                    TextContent = n.TextContent,
                    // Trả về đường dẫn tương đối (client tự ghép IP)
                    AudioUrl = string.IsNullOrEmpty(n.AudioUrl) ? "" :
                               n.AudioUrl.Replace("audios/", "").TrimStart('/'),
                    Language = new LanguageDto
                    {
                        Code = n.Language.Code,
                        Name = n.Language.Name
                    }
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (restaurant == null)
            return NotFound();

        return Ok(restaurant);
    }

    // 🔹 GET: api/restaurants/{id}/qrcode
    [HttpGet("{id}/qrcode")]
    public IActionResult GetQRCode(int id)
    {
        var exists = _context.Restaurants.Any(r => r.RestaurantId == id);
        if (!exists) return NotFound();

        var qrContent = $"tourismapp://restaurant/{id}";
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(10);

        return File(pngBytes, "image/png", $"qr-restaurant-{id}.png");
    }

    // 🔹 GET: api/restaurants/my
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyRestaurant()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);
        var restaurant = await _context.Restaurants
            .Where(r => r.OwnerId == userId)
            .Select(r => new RestaurantDto
            {
                RestaurantId = r.RestaurantId,
                Name = r.Name,
                Address = r.Address,
                Description = r.Description,
                Phone = r.Phone,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Image = r.Image,
                IsPremium = r.IsPremium,
                IsActive = r.IsActive,
                IsApproved = r.IsApproved,
                PremiumExpireDate = r.PremiumExpireDate,
                Narrations = r.Narrations.Select(n => new NarrationDto
                {
                    NarrationId = n.NarrationId,
                    LanguageId = n.LanguageId,
                    TextContent = n.TextContent,
                    AudioUrl = n.AudioUrl,
                    Language = n.Language != null ? new LanguageDto
                    {
                        Code = n.Language.Code,
                        Name = n.Language.Name
                    } : null
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (restaurant == null) return NotFound();
        return Ok(restaurant);
    }

    // 🔥 POST: api/restaurants
    [Authorize]
    [HttpPost]
    public IActionResult CreateRestaurant([FromBody] RestaurantDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Address = dto.Address,
            Description = dto.Description,
            Phone = dto.Phone,
            Image = dto.Image,

            Latitude = dto.Latitude,
            Longitude = dto.Longitude,

            IsApproved = false,
            IsPremium = false,
            CreatedAt = DateTime.Now,

            OwnerId = userId // ✅ FIX CHUẨN
        };

        _context.Restaurants.Add(restaurant);
        _context.SaveChanges();

        return Ok(new { restaurant.RestaurantId, Message = "Đăng ký nhà hàng thành công, chờ thanh toán" });
    }

    // 🔥 PUT: api/restaurants/{id}
    //[Authorize]
    //[HttpPut("{id}")]
    //public IActionResult UpdateRestaurant(int id, [FromBody] RestaurantDto dto)
    //{
    //    var restaurant = _context.Restaurants.Find(id);

    //    if (restaurant == null)
    //        return NotFound();

    //    restaurant.Name = dto.Name;
    //    restaurant.Address = dto.Address;
    //    restaurant.Description = dto.Description;
    //    restaurant.Image = dto.Image;
    //    restaurant.Phone = dto.Phone;

    //    _context.SaveChanges();

    //    return Ok(restaurant);
    //}

    // 🔹 GET: api/restaurants/{id}/dishes
    [HttpGet("{id}/dishes")]
    public IActionResult GetDishesByRestaurant(int id)
    {
        var dishes = _context.Dishes
            .Where(d => d.RestaurantId == id && d.IsActive)
            .ToList();

        return Ok(dishes);
    }

    [Authorize]
    [HttpPut("update-my")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateMyRestaurant(
        [FromForm] string name,
        [FromForm] string address,
        [FromForm] string description,
        [FromForm] string phone,
        [FromForm] string? latitude,
        [FromForm] string? longitude,
        [FromForm] List<int>? languageIds,
        [FromForm] List<string>? textContents,
        IFormFile? imageFile)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        int userId = int.Parse(userIdClaim.Value);

        var restaurant = await _context.Restaurants
            .Include(r => r.Narrations)
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound("Không tìm thấy nhà hàng");

        restaurant.Name = name;
        restaurant.Address = address;
        restaurant.Description = description;
        restaurant.Phone = phone;

        if (double.TryParse(latitude, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lat))
            restaurant.Latitude = lat;

        if (double.TryParse(longitude, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lng))
            restaurant.Longitude = lng;

        if (imageFile != null)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string path = Path.Combine(_env.WebRootPath, "images", fileName);
            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "images"))) Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "images"));
            using (var stream = new FileStream(path, FileMode.Create)) await imageFile.CopyToAsync(stream);
            restaurant.Image = "images/" + fileName;
        }

        // Xử lý nhiều thuyết minh (multi-language narrations) — tự động dịch + tạo TTS
        if (languageIds != null && languageIds.Count > 0)
        {
            var langInfos = await _context.Languages
                .Where(l => languageIds.Contains(l.LanguageId))
                .ToDictionaryAsync(l => l.LanguageId, l => l.Code);

            string audioFolder = Path.Combine(_env.ContentRootPath, "audios");
            if (!Directory.Exists(audioFolder)) Directory.CreateDirectory(audioFolder);

            string sharedUploadedAudio = null;
            const string sourceLang = "vi";

            for (int i = 0; i < languageIds.Count; i++)
            {
                var langId = languageIds[i];
                var originalText = (textContents != null && i < textContents.Count) ? textContents[i] : "";
                langInfos.TryGetValue(langId, out var langCode);
                langCode ??= "vi";
                var audioFile = Request.Form.Files.FirstOrDefault(f => f.Name == $"audioFile_{i}");

                var nar = restaurant.Narrations?.FirstOrDefault(n => n.LanguageId == langId);

                // 1. Dịch nội dung nếu ngôn ngữ đích khác nguồn
                string finalText = originalText;
                if (!string.Equals(langCode, sourceLang, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(originalText))
                {
                    try { finalText = await TranslateTextAsync(originalText, sourceLang, langCode); }
                    catch { finalText = originalText; }
                }

                // 2. Xử lý audio
                string audioUrl = nar?.AudioUrl ?? "";

                if (audioFile != null)
                {
                    // Người dùng upload file audio
                    string audioName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
                    string audioPath = Path.Combine(audioFolder, audioName);
                    using (var stream = new FileStream(audioPath, FileMode.Create)) await audioFile.CopyToAsync(stream);
                    audioUrl = audioName;
                    sharedUploadedAudio ??= audioName;
                }
                else
                {
                    // Không có file upload → tạo TTS tự động
                    try
                    {
                        string ttsFileName = $"res_{restaurant.RestaurantId}_{langCode}_{Guid.NewGuid().ToString()[..5]}.mp3";
                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                        string ttsUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(finalText)}&tl={langCode}&client=tw-ob";
                        var audioBytes = await httpClient.GetByteArrayAsync(ttsUrl);
                        await System.IO.File.WriteAllBytesAsync(Path.Combine(audioFolder, ttsFileName), audioBytes);
                        audioUrl = ttsFileName;
                    }
                    catch
                    {
                        // TTS thất bại — dùng audio đã upload hoặc giữ nguyên
                        if (sharedUploadedAudio != null && string.IsNullOrEmpty(audioUrl))
                            audioUrl = sharedUploadedAudio;
                    }
                }

                // 3. Lưu narration
                if (nar != null)
                {
                    nar.TextContent = finalText;
                    nar.AudioUrl = audioUrl;
                }
                else
                {
                    _context.Narrations.Add(new Narration
                    {
                        RestaurantId = restaurant.RestaurantId,
                        LanguageId = langId,
                        TextContent = finalText,
                        AudioUrl = audioUrl
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(restaurant);
    }


    [Authorize]
    [HttpPatch("toggle-active")]
    public async Task<IActionResult> ToggleActive([FromBody] bool status)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound("Không tìm thấy nhà hàng");

        restaurant.IsActive = status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật trạng thái thành công", isActive = restaurant.IsActive });
    }


    [HttpPatch("upgrade/{level}")]
    [Authorize] // Đảm bảo phải có cái này để lấy được thông tin User
    public async Task<IActionResult> UpgradeRestaurant(int level)
    {
        // Lấy UserId từ Token đã đăng nhập
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized("Phiên đăng nhập hết hạn");

        int currentUserId = int.Parse(userIdClaim.Value);

        // Bây giờ bạn có thể dùng currentUserId để tìm nhà hàng
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.OwnerId == currentUserId);

        if (restaurant == null) return NotFound("Không tìm thấy nhà hàng");

        restaurant.PremiumLevel = level;
        restaurant.IsPremium = true;

        if (level == 1) // Gói tháng
            restaurant.PremiumExpireDate = DateTime.Now.AddMonths(1);
        else if (level == 2) // Gói năm
            restaurant.PremiumExpireDate = DateTime.Now.AddYears(1);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Nâng cấp thành công", expireDate = restaurant.PremiumExpireDate });
    }


    [Authorize]
    [HttpGet("my/narrations")]
    public async Task<IActionResult> GetMyNarrations()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var restaurant = await _context.Restaurants
            .Include(r => r.Narrations)
                .ThenInclude(n => n.Language)
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = restaurant.Narrations.Select(n => {
            var rawAudio = n.AudioUrl ?? "";
            if (rawAudio.StartsWith("audios/", StringComparison.OrdinalIgnoreCase))
                rawAudio = rawAudio.Substring(7);
            return new {
                n.NarrationId,
                LanguageName = n.Language?.Name ?? "Unknown",
                LanguageCode = n.Language?.Code ?? "vi",
                n.TextContent,
                AudioUrl = string.IsNullOrEmpty(rawAudio)
                           ? ""
                           : rawAudio.StartsWith("http")
                               ? rawAudio
                               : $"{baseUrl}/audios/{rawAudio}"
            };
        });

        return Ok(result);
    }


    // 🔹 GET: api/restaurants/admin/all-narrations
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all-narrations")]
    public async Task<IActionResult> GetAllNarrationsForAdmin()
    {
        var narrations = await _context.Narrations
            .Include(n => n.Language)
            .Include(n => n.Dish).ThenInclude(d => d.Restaurant)
            .Include(n => n.Restaurant)
            .Select(n => new NarrationAdminDto
            { // Dùng DTO để đồng bộ với Frontend
                NarrationId = n.NarrationId,
                DishId = n.DishId ?? 0,          // 🔥 Dòng này cứu sống nút "Dịch" của bạn
                RestaurantId = n.RestaurantId,
                TextContent = n.TextContent,
                LanguageName = n.Language.Name,
                AudioUrl = n.AudioUrl,
                // Lấy tên nhà hàng dù là thuyết minh món ăn hay thuyết minh chung của nhà hàng
                RestaurantName = n.Dish != null ? n.Dish.Restaurant.Name : (n.Restaurant != null ? n.Restaurant.Name : "N/A")
            })
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        foreach (var n in narrations)
        {
            if (!string.IsNullOrEmpty(n.AudioUrl) && !n.AudioUrl.StartsWith("http"))
            {
                var rawAudio = n.AudioUrl;
                if (rawAudio.StartsWith("audios/", StringComparison.OrdinalIgnoreCase))
                    rawAudio = rawAudio.Substring(7);
                n.AudioUrl = $"{baseUrl}/audios/{rawAudio}";
            }
        }

        return Ok(narrations);
    }

    // 🔹 POST: api/restaurants/admin/update-audiox`
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/update-audio")]
    public async Task<IActionResult> AdminUpdateAudio([FromForm] int narrationId, IFormFile audioFile)
    {
        var nar = await _context.Narrations.FindAsync(narrationId);
        if (nar == null) return NotFound();

        if (audioFile != null)
        {
            string audioName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
            string audioFolder = Path.Combine(_env.ContentRootPath, "audios");
            if (!Directory.Exists(audioFolder)) Directory.CreateDirectory(audioFolder);
            string audioPath = Path.Combine(audioFolder, audioName);

            using (var stream = new FileStream(audioPath, FileMode.Create))
                await audioFile.CopyToAsync(stream);

            nar.AudioUrl = audioName; // Cập nhật tên file audio do Admin upload
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Cập nhật Audio thành công", audioUrl = nar.AudioUrl });
    }




    // Đảm bảo Route khớp với: api/restaurants/admin/delete-narration/{id}
    [HttpPost("admin/delete-narration/{id}")]
    public async Task<IActionResult> DeleteNarration(int id)
    {
        var narration = await _context.Narrations.FindAsync(id);
        if (narration == null) return NotFound();

        // 1. Xóa file âm thanh vật lý để tránh rác server
        if (!string.IsNullOrEmpty(narration.AudioUrl))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), narration.AudioUrl);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        // 2. Xóa trong DB
        _context.Narrations.Remove(narration);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // DELETE: api/restaurants/my/narrations/{id}
    [Authorize]
    [HttpDelete("my/narrations/{id}")]
    public async Task<IActionResult> DeleteMyNarration(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var restaurant = await _context.Restaurants
            .Include(r => r.Narrations)
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound();

        var narration = restaurant.Narrations?.FirstOrDefault(n => n.NarrationId == id);
        if (narration == null) return NotFound("Không tìm thấy thuyết minh");

        if (!string.IsNullOrEmpty(narration.AudioUrl) && !narration.AudioUrl.StartsWith("http"))
        {
            var filePath = Path.Combine(_env.WebRootPath, "audios", narration.AudioUrl);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        _context.Narrations.Remove(narration);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Xóa thuyết minh thành công" });
    }


    // Thêm cái này vào cuối file NarrationsController.cs của Backend
    public class NarrationAdminDto
    {
        public int NarrationId { get; set; }
        public string? TextContent { get; set; }
        public string? LanguageName { get; set; }
        public string? AudioUrl { get; set; }
        public int DishId { get; set; }
        public string? RestaurantName { get; set; }
        public int? RestaurantId { get; set; }
    }

    // 🔹 GET: api/restaurants/admin/all — List all restaurants for admin
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    public IActionResult GetAllRestaurantsForAdmin()
    {
        var restaurants = _context.Restaurants
            .Include(r => r.Owner)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.RestaurantId,
                r.Name,
                r.Address,
                r.Phone,
                r.Description,
                r.Latitude,
                r.Longitude,
                r.IsApproved,
                r.IsActive,
                r.IsPremium,
                r.CreatedAt,
                OwnerName = r.Owner != null ? r.Owner.FullName : "N/A",
                OwnerEmail = r.Owner != null ? r.Owner.Email : "N/A"
            })
            .ToList();

        return Ok(restaurants);
    }

    // 🔹 PATCH: api/restaurants/admin/{id}/approve — Toggle approval
    [Authorize(Roles = "Admin")]
    [HttpPatch("admin/{id}/approve")]
    public async Task<IActionResult> ToggleApproval(int id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        restaurant.IsApproved = !restaurant.IsApproved;
        await _context.SaveChangesAsync();

        return Ok(new { restaurant.IsApproved, message = restaurant.IsApproved ? "Đã duyệt nhà hàng" : "Đã hủy duyệt nhà hàng" });
    }


}