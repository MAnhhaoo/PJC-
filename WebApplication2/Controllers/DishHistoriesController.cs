using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Models;

[ApiController]
[Route("api/[controller]")]
public class DishHistoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DishHistoriesController(AppDbContext context)
    {
        _context = context;
    }

    // =========================================
    // 🔥 1. LƯU LỊCH SỬ NGHE
    // POST: api/dishhistories/listen/5
    // =========================================
    [Authorize]
    [HttpPost("listen/{dishId}")]
    public IActionResult SaveHistory(int dishId)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var history = new DishHistory
        {
            UserId = userId,
            DishId = dishId,
            ListenedAt = DateTime.Now
        };

        _context.DishHistories.Add(history);
        _context.SaveChanges();

        return Ok(new { message = "Đã lưu lịch sử nghe" });
    }

    // =========================================
    // 🔥 2. ĐẾM SỐ LẦN USER NGHE 1 MÓN
    // GET: api/dishhistories/count/5
    // =========================================
    [Authorize]
    [HttpGet("count/{dishId}")]
    public IActionResult CountListen(int dishId)
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var count = _context.DishHistories
            .Count(h => h.UserId == userId && h.DishId == dishId);

        return Ok(new
        {
            dishId,
            listenCount = count
        });
    }

    // =========================================
    // 🔥 3. TỔNG SỐ LẦN NGHE
    // GET: api/dishhistories/total
    // =========================================
    [Authorize]
    [HttpGet("total")]
    public IActionResult TotalListen()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var total = _context.DishHistories
            .Count(h => h.UserId == userId);

        return Ok(new { total });
    }

    // =========================================
    // 🔥 4. TOP MÓN HOT (TOÀN HỆ THỐNG)
    // GET: api/dishhistories/top-dishes
    // =========================================
    [HttpGet("top-dishes")]
    public IActionResult GetTopDishes(int top = 5)
    {
        var result = _context.DishHistories
            .GroupBy(h => h.DishId)
            .Select(g => new
            {
                DishId = g.Key,
                ListenCount = g.Count()
            })
            .OrderByDescending(x => x.ListenCount)
            .Take(top)
            .Join(_context.Dishes,
                  h => h.DishId,
                  d => d.DishId,
                  (h, d) => new
                  {
                      d.DishId,
                      d.Name,
                      d.ImageUrl,
                      h.ListenCount
                  })
            .ToList();

        return Ok(result);
    }

    // =========================================
    // 🔥 5. GỢI Ý MÓN CHO USER
    // GET: api/dishhistories/recommend
    // =========================================
    [Authorize]
    [HttpGet("recommend")]
    public IActionResult Recommend()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        // 🔥 1. Lấy danh sách món user đã nghe (theo số lần nghe)
        var recommended = _context.DishHistories
            .Where(h => h.UserId == userId)
            .GroupBy(h => h.DishId)
            .Select(g => new
            {
                DishId = g.Key,
                ListenCount = g.Count()
            })
            .OrderByDescending(x => x.ListenCount)
            .Join(_context.Dishes,
                  h => h.DishId,
                  d => d.DishId,
                  (h, d) => new
                  {
                      d.DishId,
                      d.Name,
                      d.ImageUrl,
                      h.ListenCount
                  })
            .ToList();

        // 🔥 2. Nếu user chưa có lịch sử → fallback top món hot
        if (!recommended.Any())
        {
            recommended = _context.DishHistories
                .GroupBy(h => h.DishId)
                .Select(g => new
                {
                    DishId = g.Key,
                    ListenCount = g.Count()
                })
                .OrderByDescending(x => x.ListenCount)
                .Join(_context.Dishes,
                      h => h.DishId,
                      d => d.DishId,
                      (h, d) => new
                      {
                          d.DishId,
                          d.Name,
                          d.ImageUrl,
                          h.ListenCount
                      })
                .Take(5)
                .ToList();
        }

        return Ok(recommended);
    }

}