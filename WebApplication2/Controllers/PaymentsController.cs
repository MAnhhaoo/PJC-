using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.DTOs;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }
    // GET: api/payments/my-payments USER: chỉ xem payment của chính mình
    [Authorize]
    [HttpGet("my-payments")]
    public IActionResult MyPayments()
    {
        var userId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var payments = _context.Payments
       .Where(p => p.UserId == userId)
       .Select(p => new PaymentDto
       {
           PaymentId = p.PaymentId,
           Amount = p.Amount,
           PaymentType = p.PaymentType,
           PaymentDate = p.PaymentDate,
           Status = p.Status
       })
       .ToList();


        return Ok(payments);
    }

    // ADMIN: xem tất cả payments
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public IActionResult GetAllPayments()
    {
        return Ok(_context.Payments.ToList());
    }


    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_context.Payments.ToList());
    }


    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public IActionResult GetPaymentsForAdmin(
     string? search,
     DateTime? from,
     DateTime? to,
     int page = 1,
     int pageSize = 10)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Restaurant)
            .AsQueryable();

        // SEARCH
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.User.FullName.Contains(search) ||
                p.User.Email.Contains(search));
        }

        // FILTER FROM
        if (from.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= from.Value.Date);
        }

        // FILTER TO

        if (to.HasValue)
        {
            var toDate = to.Value.Date.AddDays(1);
            query = query.Where(p => p.PaymentDate < toDate);
        }

        var totalRecords = query.Count();

        var payments = query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.PaymentId,
                p.Amount,
                p.PaymentType,
                p.PaymentDate,
                p.Status,
                UserName = p.User.FullName,
                Email = p.User.Email,
                RestaurantName = p.Restaurant != null ? p.Restaurant.Name : null
            })
            .ToList();

        return Ok(new
        {
            totalRecords,
            page,
            pageSize,
            data = payments
        });
    }



    //[Authorize(Roles = "Admin")]
    //[HttpGet("admin/stats")]
    //public IActionResult GetPaymentStats()
    //{
    //    var successPayments = _context.Payments
    //        .Where(p => p.Status == "Success");

    //    var userRevenue = successPayments
    //        .Where(p => p.PaymentType == "UserUpgrade")
    //        .Sum(p => p.Amount);

    //    var restaurantRevenue = successPayments
    //        .Where(p => p.PaymentType == "RestaurantPremium")
    //        .Sum(p => p.Amount);

    //    var totalSuccess = successPayments.Count();

    //    return Ok(new
    //    {
    //        totalSuccess,
    //        userRevenue,
    //        restaurantRevenue,
    //        totalRevenue = userRevenue + restaurantRevenue
    //    });
    //}


    [Authorize(Roles = "Admin")]
    [HttpGet("admin/{id}")]
    public IActionResult GetPaymentDetail(int id)
    {
        var payment = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Restaurant)
            .FirstOrDefault(p => p.PaymentId == id);

        if (payment == null)
            return NotFound();

        return Ok(new
        {
            payment.PaymentId,
            payment.Amount,
            payment.PaymentType,
            payment.PaymentDate,
            payment.Status,
            payment.PaymentMethod,
            payment.TransactionId,
            payment.ExpireDate,
            UserName = payment.User.FullName,
            Email = payment.User.Email,
            RestaurantName = payment.Restaurant != null ? payment.Restaurant.Name : null
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/stats")]
    public IActionResult GetStats(DateTime? from, DateTime? to)
    {
        var query = _context.Payments
            .Where(p => p.Status == "Success");

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to);

        var totalSuccess = query.Count();

        var userRevenue = query
            .Where(p => p.PaymentType.Contains("User"))
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var restaurantRevenue = query
            .Where(p => p.PaymentType.Contains("Restaurant"))
            .Sum(p => (decimal?)p.Amount) ?? 0;

        return Ok(new
        {
            totalSuccess,
            userRevenue,
            restaurantRevenue,
            totalRevenue = userRevenue + restaurantRevenue
        });
    }

    [Authorize(Roles = "Admin")]
[HttpGet("admin/monthly-revenue")]
public IActionResult MonthlyRevenue()
{
    var sixMonthsAgo = DateTime.Now.AddMonths(-6);

    var data = _context.Payments
        .Where(p => p.Status == "Success" && p.PaymentDate >= sixMonthsAgo)
        .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
        .Select(g => new
        {
            Year = g.Key.Year,
            Month = g.Key.Month,
            Total = g.Sum(p => p.Amount)
        })
        .OrderBy(x => x.Year)
        .ThenBy(x => x.Month)
        .ToList();

    return Ok(data);
}



}
