using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NarrationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NarrationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/narrations/dish/5
        [HttpGet("dish/{dishId}")]
        public IActionResult GetByDish(int dishId)
        {
            // Chúng ta trả về danh sách theo đúng cấu trúc DTO mà Frontend mong đợi
            var narrations = _context.Narrations
                .Where(n => n.DishId == dishId)
                .Include(n => n.Language)
                .Include(n => n.Dish)
                    .ThenInclude(d => d.Restaurant)
                .Select(n => new {
                    // Đảm bảo tên các trường này KHỚP HOÀN TOÀN với NarrationAdminDto ở FE
                    NarrationId = n.NarrationId,
                    TextContent = n.TextContent,
                    AudioUrl = n.AudioUrl,
                    LanguageName = n.Language.Name,
                    LanguageCode = n.Language.Code,
                    DishId = n.DishId, // Trả về số ID thật từ DB
                    RestaurantName = n.Dish != null && n.Dish.Restaurant != null ? n.Dish.Restaurant.Name : "",
                    RestaurantId = n.RestaurantId
                })
                .ToList();

            return Ok(narrations);
        }

        // ✅ API PROXY DỊCH THUẬT: api/narrations/proxy-translate
        [HttpGet("proxy-translate")]
        public async Task<IActionResult> ProxyTranslate([FromQuery] string text, [FromQuery] string langPair)
        {
            if (string.IsNullOrEmpty(text)) return BadRequest("Text is required");

            using var client = new HttpClient();
            // Thêm User-Agent để MyMemory không chặn vì nghi ngờ là Bot
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={langPair}";

            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                return StatusCode((int)response.StatusCode, "Lỗi từ máy chủ dịch thuật");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPost("create-translated")]
        public async Task<IActionResult> CreateTranslated([FromBody] NarrationCreateDto dto)
        {
            var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == dto.LanguageCode);
            if (language == null) return BadRequest("Ngôn ngữ không hợp lệ");

            // 1. Tạo file Audio tự động
            string fileName = $"res_{dto.RestaurantId}_{Guid.NewGuid().ToString().Substring(0, 5)}.mp3";
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "audios");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            try
            {
                using var client = new HttpClient();
                // Google TTS: tl = ngôn ngữ (vi, en, zh...)
                string ttsUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(dto.TextContent)}&tl={dto.LanguageCode}&client=tw-ob";
                var audioBytes = await client.GetByteArrayAsync(ttsUrl);
                await System.IO.File.WriteAllBytesAsync(Path.Combine(folderPath, fileName), audioBytes);
            }
            catch
            {
                fileName = ""; // Lỗi TTS thì để trống link
            }

            // 2. Lưu vào Database
            var newNarration = new Narration
            {
                RestaurantId = dto.RestaurantId,
                DishId = dto.DishId, // Sẽ là null nếu Admin tạo cho nhà hàng
                TextContent = dto.TextContent,
                LanguageId = language.LanguageId,
                AudioUrl = string.IsNullOrEmpty(fileName) ? "" : $"audios/{fileName}"
            };

            _context.Narrations.Add(newNarration);
            await _context.SaveChangesAsync();
            return Ok();
        }





        // Khai báo DTO để nhận dữ liệu
        public class NarrationCreateDto
        {
            public int? DishId { get; set; }
            public string TextContent { get; set; } = "";
            public string LanguageCode { get; set; } = "";
            public int? RestaurantId { get; set; }
        }
    }


}