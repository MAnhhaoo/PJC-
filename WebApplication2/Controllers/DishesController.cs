using Microsoft.AspNetCore.Mvc;
using WebApplication2.Data;

[ApiController]
[Route("api/[controller]")]
public class DishesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DishesController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 GET: api/dishes
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_context.Dishes.ToList());
    }
}
