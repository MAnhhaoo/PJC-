using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.Models;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;  

    public DishesController(AppDbContext context , IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }




    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] string name,
        [FromForm] string price,
        [FromForm] string description,
        [FromForm] int? languageId,       // Nhận ID ngôn ngữ từ App
        [FromForm] string textContent,    // Nội dung text của thuyết minh
        IFormFile image,                  // Ảnh món ăn
        IFormFile audioFile)              // File audio thuyết minh
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int userId = int.Parse(userIdClaim.Value);
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
        if (restaurant == null) return BadRequest("Bạn chưa có nhà hàng");

        if (image == null || image.Length == 0) return BadRequest("Vui lòng chọn ảnh món ăn");

        // Sử dụng Transaction để đảm bảo nếu lỗi Audio thì không lưu Dish (và ngược lại)
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Xử lý lưu Ảnh món ăn vào wwwroot/images
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            string imageFolder = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(imageFolder)) Directory.CreateDirectory(imageFolder);

            string imagePath = Path.Combine(imageFolder, fileName);
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // 2. Khởi tạo đối tượng Dish
            var dish = new Dish
            {
                Name = name,
                ImageUrl = "http://10.0.2.2:5216/images/" + fileName,
                RestaurantId = restaurant.RestaurantId,
                IsActive = true,
                Narrations = null, // Khởi tạo tạm
                DishHistories = null
            };

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync(); // Lưu để lấy DishId

            // 3. Xử lý lưu Audio và Narration nếu có
            if (audioFile != null && languageId.HasValue)
            {
                string audioFileName = Guid.NewGuid().ToString() + Path.GetExtension(audioFile.FileName);
                string audioFolder = Path.Combine(_env.WebRootPath, "audios");
                if (!Directory.Exists(audioFolder)) Directory.CreateDirectory(audioFolder);

                string audioPath = Path.Combine(audioFolder, audioFileName);
                using (var stream = new FileStream(audioPath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                var narration = new Narration
                {
                    DishId = dish.DishId,
                    LanguageId = languageId.Value,
                    TextContent = textContent ?? "",
                    AudioUrl = "http://10.0.2.2:5216/audios/" + audioFileName
                };

                _context.Narrations.Add(narration);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return Ok(dish);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Lỗi khi lưu dữ liệu: " + ex.Message);
        }
    }





    [Authorize]
    [HttpGet("my-dishes")]
    //public async Task<IActionResult> GetMyDishes()
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

    //    if (userIdClaim == null)
    //        return Unauthorized();

    //    int userId = int.Parse(userIdClaim.Value);

    //    // 🔥 lấy restaurant của user
    //    var restaurant = await _context.Restaurants
    //        .FirstOrDefaultAsync(r => r.OwnerId == userId);

    //    if (restaurant == null)
    //        return NotFound("Bạn chưa có nhà hàng");

    //    // 🔥 chỉ lấy món của nhà hàng đó
    //    var dishes = await _context.Dishes
    //        .Where(d => d.RestaurantId == restaurant.RestaurantId)
    //        .ToListAsync();

    //    return Ok(dishes);
    //}


    // 🔹 GET: api/dishes
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_context.Dishes.ToList());
    }

    // 🔥 POST: api/dishes (THÊM MÓN)
    

    // 🔥 PUT: api/dishes/{id} (SỬA MÓN)
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DishUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var dish = await _context.Dishes.Include(d => d.Restaurant).FirstOrDefaultAsync(d => d.DishId == id);
        if (dish == null || dish.Restaurant.OwnerId != userId) return Forbid();

        dish.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.ImageUrl)) dish.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync();
        return Ok(dish);
    }
    //public class DishInputDto
    //{
    //    public string Name { get; set; }
    //    public string ImageUrl { get; set; }
    //    public string Price { get; set; }        // Thêm trường này
    //    public string Description { get; set; }  // Thêm trường này
    //}

    // 🔥 PUT: api/dishes/{id}/toggle (BẬT/TẮT)
    [Authorize]
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> ToggleDish(int id, [FromBody] ToggleDishDto data)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        var dish = await _context.Dishes
            .Include(d => d.Restaurant)
            .FirstOrDefaultAsync(d => d.DishId == id);

        if (dish == null)
            return NotFound();

        // 🔥 check đúng owner
        if (dish.Restaurant.OwnerId != userId)
            return Forbid();

        dish.IsActive = data.IsActive;

        await _context.SaveChangesAsync();

        return Ok(dish);
    }
    // 🔥 PUT: api/dishes/{id} (SỬA MÓN)
    //[Authorize]
    //[HttpPut("{id}")]
    //public async Task<IActionResult> Update(int id, [FromBody] Dish dto)
    //{
    //    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

    //    var dish = await _context.Dishes
    //        .Include(d => d.Restaurant)
    //        .FirstOrDefaultAsync(d => d.DishId == id);

    //    if (dish == null)
    //        return NotFound();

    //    if (dish.Restaurant.OwnerId != userId)
    //        return Forbid();

    //    dish.Name = dto.Name;
    //    dish.ImageUrl = dto.ImageUrl;

    //    await _context.SaveChangesAsync();

    //    return Ok(dish);
    //}

    // 🔥 DELETE: api/dishes/{id}
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var dish = _context.Dishes.Find(id);

        if (dish == null)
            return NotFound();

        _context.Dishes.Remove(dish);
        _context.SaveChanges();

        return Ok("Xóa thành công");
    }

    // 🔥 GET: api/dishes/my
    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyDishes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return Unauthorized();

        int userId = int.Parse(userIdClaim.Value);

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.OwnerId == userId);

        if (restaurant == null) return NotFound("Bạn chưa có nhà hàng");

        var dishes = await _context.Dishes
            .Where(d => d.RestaurantId == restaurant.RestaurantId)
            .Select(d => new
            {
                d.DishId,
                d.Name,
                d.ImageUrl,
                d.IsActive
            })
            .ToListAsync();

        return Ok(dishes);
    }
}