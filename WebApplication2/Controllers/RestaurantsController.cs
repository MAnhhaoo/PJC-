using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.DTOs;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RestaurantsController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 GET: api/restaurants (VIP user xem tất cả còn hạn)
    [HttpGet]
    public IActionResult GetRestaurants()
    {
        var restaurants = _context.Restaurants
            .Where(r =>
                r.IsApproved && // restaurant nomal
               (!r.IsPremium || r.PremiumExpireDate == null || r.PremiumExpireDate > DateTime.Now) // restaurant premium
            )
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
               IsApproved = r.IsApproved   // 🔥 THÊM DÒNG NÀY
           })
            .ToList();

        return Ok(restaurants);
    }

    // 🔹 GET: api/restaurants/normal (User Free chỉ xem nhà hàng thường)
    [HttpGet("normal")]
    public IActionResult GetNormalRestaurants()
    {
        var restaurants = _context.Restaurants
            .Where(r =>
                r.IsApproved &&
                !r.IsPremium
            )
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
               IsApproved = r.IsApproved   // 🔥 thêm dòng này
           })
            .ToList();

        return Ok(restaurants);
    }

    // 🔹 GET: api/restaurants/{id}/dishes
    [HttpGet("{id}/dishes")]
    public IActionResult GetDishesByRestaurant(int id)
    {
        var dishes = _context.Dishes
            .Where(d => d.RestaurantId == id && d.IsActive)
            .ToList();

        return Ok(dishes);
    }


    [HttpGet("my")]
    public async Task<IActionResult> GetMyRestaurant()
    {
        var userId = 1; // tạm test

        var restaurant = await _context.Restaurants
            .Where(r => r.OwnerId == userId)
            .Select(r => new
            {
                r.RestaurantId,
                r.Name,
                r.Address,
                r.Description,
                r.Image,
                r.IsPremium,
                r.PremiumExpireDate
            })
            .FirstOrDefaultAsync();

        if (restaurant == null)
            return NotFound();

        return Ok(restaurant);
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
}