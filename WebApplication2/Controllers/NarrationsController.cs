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
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
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
                DishId = dto.DishId,
                TextContent = dto.TextContent,
                LanguageId = language.LanguageId,
                AudioUrl = string.IsNullOrEmpty(fileName) ? "" : fileName
            };

            _context.Narrations.Add(newNarration);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("create-translated-multi")]
        public async Task<IActionResult> CreateTranslatedMulti([FromBody] NarrationCreateMultiDto dto)
        {
            if (dto.LanguageCodes == null || !dto.LanguageCodes.Any())
                return BadRequest("No language codes provided");

            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "audios");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var created = 0;

            // assume source text is Vietnamese (vi). If you need dynamic source, extend DTO with SourceLang.
            const string sourceLang = "vi";
            foreach (var code in dto.LanguageCodes.Distinct())
            {
                try
                {
                    var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == code);
                    if (language == null) continue; // skip unknown languages
                    // Translate text if target language differs from source
                    string textForTts = dto.TextContent ?? string.Empty;
                    if (!string.Equals(code, sourceLang, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            textForTts = await TranslateTextAsync(dto.TextContent ?? string.Empty, sourceLang, code);
                        }
                        catch
                        {
                            // fallback to original text if translation fails
                            textForTts = dto.TextContent ?? string.Empty;
                        }
                    }

                    string fileName = $"res_{dto.RestaurantId}_{code}_{Guid.NewGuid().ToString().Substring(0, 5)}.mp3";
                    var savedFile = string.Empty;
                    try
                    {
                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                        string ttsUrl = $"https://translate.google.com/translate_tts?ie=UTF-8&q={Uri.EscapeDataString(textForTts)}&tl={code}&client=tw-ob";
                        var audioBytes = await client.GetByteArrayAsync(ttsUrl);
                        var fullPath = Path.Combine(folderPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(fullPath, audioBytes);
                        savedFile = fileName;
                    }
                    catch
                    {
                        // tts failed for this language
                        savedFile = string.Empty;
                    }

                    var newNarration = new Narration
                    {
                        RestaurantId = dto.RestaurantId,
                        DishId = dto.DishId,
                        TextContent = textForTts,
                        LanguageId = language.LanguageId,
                        AudioUrl = string.IsNullOrEmpty(savedFile) ? string.Empty : Path.GetFileName(savedFile)
                    };

                    _context.Narrations.Add(newNarration);
                    created++;
                }
                catch
                {
                    // ignore individual failures
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { created });
        }





        // Khai báo DTO để nhận dữ liệu
        public class NarrationCreateDto
        {
            public int? DishId { get; set; }
            public string TextContent { get; set; } = "";
            public string LanguageCode { get; set; } = "";
            public int? RestaurantId { get; set; }
        }

        public class NarrationCreateMultiDto
        {
            public int? DishId { get; set; }
            public string TextContent { get; set; } = "";
            public List<string>? LanguageCodes { get; set; }
            public int? RestaurantId { get; set; }
        }
    }


}