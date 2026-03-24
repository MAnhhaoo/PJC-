using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.Models;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ GET: api/reviews/restaurant/5
    [HttpGet("restaurant/{restaurantId}")]
    public IActionResult GetByRestaurant(int restaurantId)
    {
        var reviews = _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReviewId,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                UserName = r.User.FullName,
                Avatar = r.User.Avatar
            })
            .ToList();

        return Ok(reviews);
    }

    // ✅ POST: api/reviews
    [Authorize]
    [HttpPost]
    public IActionResult Create([FromBody] Review model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var review = new Review
        {
            UserId = userId,
            RestaurantId = model.RestaurantId,
            Rating = model.Rating,
            Comment = model.Comment,
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);
        _context.SaveChanges();

        return Ok(new { message = "Đánh giá thành công" });
    }
}