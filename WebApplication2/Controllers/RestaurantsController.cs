using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                    TextContent = n.TextContent,
                    // Tạo URL tuyệt đối cho Audio
                    AudioUrl = string.IsNullOrEmpty(n.AudioUrl) ? "" :
                               (n.AudioUrl.StartsWith("http") ? n.AudioUrl : $"http://10.0.2.2:5216/audios/{n.AudioUrl}"),
                    Language = new LanguageDto
                    {
                        Code = n.Language.Code,
                        Name = n.Language.Name
                    }
                }).ToList()
            })
            .ToList();

        return Ok(restaurants);
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
                IsApproved = r.IsApproved
            })
            .FirstOrDefaultAsync();

        if (restaurant == null)
            return NotFound();

        return Ok(restaurant);
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

        return Ok("Đăng ký nhà hàng thành công, chờ duyệt");
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
    [FromForm] double latitude,
    [FromForm] double longitude,
    [FromForm] int? languageId,
    [FromForm] string textContent,
   IFormFile? imageFile, // Sửa thành IFormFile?
    IFormFile? audioFile)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();
        int userId = int.Parse(userIdClaim.Value);

        var restaurant = await _context.Restaurants
            .Include(r => r.Narrations)
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound("Không tìm thấy nhà hàng");

        // Cập nhật thông tin cơ bản
        restaurant.Name = name;
        restaurant.Address = address;
        restaurant.Description = description;
        restaurant.Phone = phone;
        restaurant.Latitude = latitude;
        restaurant.Longitude = longitude;

        // 1. Xử lý Ảnh nhà hàng
        if (imageFile != null)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            string path = Path.Combine(_env.WebRootPath, "images", fileName);
            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "images"))) Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "images"));
            using (var stream = new FileStream(path, FileMode.Create)) await imageFile.CopyToAsync(stream);
            restaurant.Image = "http://10.0.2.2:5216/images/" + fileName;
        }

        // 2. Xử lý Thuyết minh (Narration)
        if (languageId.HasValue)
        {
            var nar = restaurant.Narrations?.FirstOrDefault(n => n.LanguageId == languageId.Value);
            string audioUrl = nar?.AudioUrl ?? "";

            // Nếu có file Audio mới thì upload, nếu không có thì giữ nguyên AudioUrl cũ (hoặc trống)
            // Trong hàm UpdateMyRestaurant, đoạn xử lý Audio
            if (audioFile != null)
            {
                string audioName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
                string audioFolder = Path.Combine(_env.WebRootPath, "audios");
                if (!Directory.Exists(audioFolder)) Directory.CreateDirectory(audioFolder);

                string audioPath = Path.Combine(audioFolder, audioName);
                using (var stream = new FileStream(audioPath, FileMode.Create)) await audioFile.CopyToAsync(stream);

                // CHỈ LƯU TÊN FILE VÀO BIẾN audioUrl
                audioUrl = audioName;
            }

            if (nar != null) // Cập nhật bản cũ
            {
                nar.TextContent = textContent ?? "";
                nar.AudioUrl = audioUrl;
            }
            else // Thêm mới bản thuyết minh cho ngôn ngữ này
            {
                _context.Narrations.Add(new Narration
                {
                    RestaurantId = restaurant.RestaurantId,
                    LanguageId = languageId.Value,
                    TextContent = textContent ?? "",
                    AudioUrl = audioUrl
                });
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
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound();

        var result = restaurant.Narrations.Select(n => new {
            n.NarrationId,
            LanguageName = n.LanguageId == 1 ? "Tiếng Việt" : "English",
            n.TextContent,
            // Tạo URL chuẩn: Nếu là tên file thì nối thêm host, nếu đã có http thì giữ nguyên
            AudioUrl = (string.IsNullOrEmpty(n.AudioUrl) || n.AudioUrl.StartsWith("http"))
                       ? n.AudioUrl
                       : $"http://10.0.2.2:5216/audios/{n.AudioUrl}"
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
            string audioPath = Path.Combine(_env.WebRootPath, "audios", audioName);

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


}