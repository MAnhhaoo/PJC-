using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RestaurantsController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 GET: api/restaurants
    [HttpGet]
    public IActionResult GetRestaurants()
    {
        return Ok(_context.Restaurants.ToList());
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
}
