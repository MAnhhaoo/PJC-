using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;

[ApiController]
[Route("api/[controller]")]
public class NarrationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NarrationsController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ GET: api/narrations/dish/5
    [HttpGet("dish/{dishId}")]
    public IActionResult GetByDish(int dishId)
    {
        var narrations = _context.Narrations
            .Where(n => n.DishId == dishId)
            .Include(n => n.Language)
            .Select(n => new
            {
                n.NarrationId,
                n.TextContent,
                n.AudioUrl,
                Language = n.Language.Name,
                LanguageCode = n.Language.Code
            })
            .ToList();

        return Ok(narrations);
    }
}