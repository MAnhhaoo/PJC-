using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.DTOs;
using WebApplication2.Models;


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
            .Include(p => p.Tour)
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
                p.PaymentMethod,
                p.TransactionId,
                UserName = p.User.FullName,
                Email = p.User.Email,
                RestaurantName = p.Restaurant != null ? p.Restaurant.Name : null,
                TourName = p.Tour != null ? p.Tour.Name : null
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
            .Include(p => p.Tour)
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
            RestaurantName = payment.Restaurant != null ? payment.Restaurant.Name : null,
            TourName = payment.Tour != null ? payment.Tour.Name : null
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
        var today = DateTime.Today;

        // Lấy ngày đầu tiên của tháng hiện tại
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);

        // Lùi 5 tháng để đủ 6 tháng gần nhất
        var sixMonthsAgo = firstDayThisMonth.AddMonths(-5);

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


    [Authorize]
    [HttpPost("confirm/{id}")]
    public async Task<IActionResult> ConfirmPayment(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Restaurant)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
            return NotFound("Payment not found");

        if (payment.Status == "Success")
            return BadRequest("Payment already confirmed");

        // Cập nhật trạng thái
        payment.Status = "Success";
        payment.PaymentDate = DateTime.Now;

        // ===== NÂNG CẤP USER VIP =====
        if (payment.PaymentType == "UserUpgrade")
        {
            payment.User.UserLevel = 1;
        }

        // ===== NÂNG CẤP RESTAURANT PREMIUM =====
        if (payment.PaymentType == "RestaurantPremium" && payment.Restaurant != null)
        {
            payment.Restaurant.IsPremium = true;

            // Nếu có ExpireDate từ Payment thì dùng
            if (payment.ExpireDate.HasValue)
            {
                payment.Restaurant.PremiumExpireDate = payment.ExpireDate;
            }
            else
            {
                payment.Restaurant.PremiumExpireDate = DateTime.Now.AddMonths(1);
            }
        }

        // ===== DUYỆT ĐĂNG KÝ NHÀ HÀNG =====
        if (payment.PaymentType == "RestaurantRegistration" && payment.Restaurant != null)
        {
            payment.Restaurant.IsApproved = true;
        }

        // ===== ĐĂNG KÝ NHÀ HÀNG PREMIUM =====
        if (payment.PaymentType == "RestaurantRegistrationPremium" && payment.Restaurant != null)
        {
            payment.Restaurant.IsApproved = true;
            payment.Restaurant.IsPremium = true;
            payment.Restaurant.PremiumExpireDate = payment.ExpireDate ?? DateTime.Now.AddYears(1);
        }

        await _context.SaveChangesAsync();

        return Ok("Payment confirmed successfully");
    }

    // POST: api/payments/purchase-tour
    [Authorize]
    [HttpPost("purchase-tour")]
    public async Task<IActionResult> PurchaseTour([FromBody] TourPurchaseRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tour = await _context.Tours.FindAsync(request.TourId);
        if (tour == null) return NotFound("Tour không tồn tại");

        if (tour.Price == 0)
            return BadRequest("Tour này miễn phí, không cần thanh toán");

        // Check if already purchased
        var existing = await _context.Payments.AnyAsync(p =>
            p.UserId == userId && p.TourId == request.TourId && p.Status == "Success");
        if (existing)
            return BadRequest("Bạn đã mua tour này rồi");

        // Check if there's a pending payment
        var pending = await _context.Payments.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.TourId == request.TourId && p.Status == "Pending");
        if (pending != null)
            return Ok(new { pending.PaymentId, ReferenceCode = pending.TransactionId ?? $"TPA{pending.PaymentId}", Amount = pending.Amount, Message = "Đã có thanh toán đang chờ" });

        var payment = new Payment
        {
            UserId = userId,
            TourId = request.TourId,
            Amount = tour.Price,
            PaymentType = "TourPurchase",
            PaymentDate = DateTime.Now,
            Status = "Pending",
            PaymentMethod = request.PaymentMethod ?? "QR"
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Set reference code using PaymentId for bank transfer matching
        payment.TransactionId = $"TPA{payment.PaymentId}";
        await _context.SaveChangesAsync();

        return Ok(new { payment.PaymentId, ReferenceCode = payment.TransactionId, Amount = payment.Amount, Message = "Thanh toán đang chờ xử lý" });
    }

    // GET: api/payments/check-tour/{tourId}
    [Authorize]
    [HttpGet("check-tour/{tourId}")]
    public async Task<IActionResult> CheckTourAccess(int tourId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tour = await _context.Tours.FindAsync(tourId);
        if (tour == null) return NotFound();

        if (tour.Price == 0)
            return Ok(new { IsPurchased = true, Status = "Free" });

        var payment = await _context.Payments
            .Where(p => p.UserId == userId && p.TourId == tourId)
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync();

        if (payment == null)
            return Ok(new { IsPurchased = false, Status = "NotPurchased" });

        return Ok(new
        {
            IsPurchased = payment.Status == "Success",
            Status = payment.Status
        });
    }

    // POST: api/payments/register-restaurant
    [Authorize]
    [HttpPost("register-restaurant")]
    public async Task<IActionResult> RegisterRestaurantPayment([FromBody] RestaurantPaymentRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var restaurant = await _context.Restaurants.FindAsync(request.RestaurantId);
        if (restaurant == null) return NotFound("Nhà hàng không tồn tại");

        if (restaurant.OwnerId != userId)
            return Forbid();

        // Check pending
        var pending = await _context.Payments.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.RestaurantId == request.RestaurantId
            && (p.PaymentType == "RestaurantRegistration" || p.PaymentType == "RestaurantRegistrationPremium")
            && p.Status == "Pending");
        if (pending != null)
            return Ok(new { pending.PaymentId, ReferenceCode = pending.TransactionId ?? $"TPA{pending.PaymentId}", Amount = pending.Amount, Message = "Đã có thanh toán đang chờ" });

        var isPremium = string.Equals(request.PlanType, "Premium", StringComparison.OrdinalIgnoreCase);

        var payment = new Payment
        {
            UserId = userId,
            RestaurantId = request.RestaurantId,
            Amount = request.Amount,
            PaymentType = isPremium ? "RestaurantRegistrationPremium" : "RestaurantRegistration",
            PaymentDate = DateTime.Now,
            Status = "Pending",
            PaymentMethod = request.PaymentMethod ?? "QR",
            ExpireDate = isPremium ? DateTime.Now.AddYears(1) : null
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Set reference code using PaymentId for bank transfer matching
        payment.TransactionId = $"TPA{payment.PaymentId}";
        await _context.SaveChangesAsync();

        return Ok(new { payment.PaymentId, ReferenceCode = payment.TransactionId, Amount = payment.Amount, Message = "Thanh toán đang chờ xử lý" });
    }

    // GET: api/payments/check-restaurant/{restaurantId}
    [Authorize]
    [HttpGet("check-restaurant/{restaurantId}")]
    public async Task<IActionResult> CheckRestaurantPayment(int restaurantId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var payment = await _context.Payments
            .Where(p => p.UserId == userId && p.RestaurantId == restaurantId
                && (p.PaymentType == "RestaurantRegistration" || p.PaymentType == "RestaurantRegistrationPremium"))
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync();

        if (payment == null)
            return Ok(new { IsPaid = false, Status = "NotPaid" });

        return Ok(new
        {
            IsPaid = payment.Status == "Success",
            Status = payment.Status
        });
    }

    // DELETE: api/payments/cancel-restaurant/{restaurantId}
    [Authorize]
    [HttpDelete("cancel-restaurant/{restaurantId}")]
    public async Task<IActionResult> CancelRestaurantPayment(int restaurantId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var pending = await _context.Payments.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.RestaurantId == restaurantId
            && (p.PaymentType == "RestaurantRegistration" || p.PaymentType == "RestaurantRegistrationPremium")
            && p.Status == "Pending");

        if (pending != null)
        {
            _context.Payments.Remove(pending);
        }

        // Also remove the restaurant since registration was cancelled
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r =>
            r.OwnerId == userId && r.RestaurantId == restaurantId && !r.IsApproved);
        if (restaurant != null)
        {
            _context.Restaurants.Remove(restaurant);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Payment and restaurant registration cancelled" });
    }

    // POST: api/payments/webhook
    // Called by bank monitoring services (SePay, Casso, etc.) when a transfer is received
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> PaymentWebhook([FromBody] BankWebhookPayload payload)
    {
        if (payload == null || string.IsNullOrEmpty(payload.Content))
            return BadRequest("Invalid payload");

        // Extract reference code from transfer content (pattern: TPA followed by digits)
        var match = System.Text.RegularExpressions.Regex.Match(
            payload.Content, @"TPA(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            return Ok(new { success = false, message = "No matching reference found" });

        var paymentId = int.Parse(match.Groups[1].Value);
        var payment = await _context.Payments
            .Include(p => p.Restaurant)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            return Ok(new { success = false, message = "Payment not found" });

        if (payment.Status == "Success")
            return Ok(new { success = true, message = "Already confirmed" });

        // Verify amount matches
        if (payload.TransferAmount < payment.Amount)
            return Ok(new { success = false, message = "Amount insufficient" });

        // Confirm payment
        payment.Status = "Success";
        payment.PaymentDate = DateTime.Now;

        // Handle specific payment types
        if (payment.PaymentType == "RestaurantRegistration" && payment.Restaurant != null)
        {
            payment.Restaurant.IsApproved = true;
        }
        else if (payment.PaymentType == "RestaurantRegistrationPremium" && payment.Restaurant != null)
        {
            payment.Restaurant.IsApproved = true;
            payment.Restaurant.IsPremium = true;
            payment.Restaurant.PremiumExpireDate = payment.ExpireDate ?? DateTime.Now.AddYears(1);
        }
        else if (payment.PaymentType == "RestaurantPremium" && payment.Restaurant != null)
        {
            payment.Restaurant.IsPremium = true;
            payment.Restaurant.PremiumExpireDate = payment.ExpireDate ?? DateTime.Now.AddMonths(1);
        }
        else if (payment.PaymentType == "UserUpgrade" && payment.User != null)
        {
            payment.User.UserLevel = 1;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Payment confirmed via webhook" });
    }

    // DELETE: api/payments/cancel-tour/{tourId}
    // Cancel a pending tour payment (user exited without paying)
    [Authorize]
    [HttpDelete("cancel-tour/{tourId}")]
    public async Task<IActionResult> CancelTourPayment(int tourId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var pending = await _context.Payments.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.TourId == tourId && p.Status == "Pending");

        if (pending == null)
            return Ok(new { message = "No pending payment found" });

        _context.Payments.Remove(pending);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment cancelled" });
    }

    // GET: api/payments/restaurant-dish-limit/{restaurantId}
    // Check dish limit for restaurant (normal = 8, premium = unlimited)
    [Authorize]
    [HttpGet("restaurant-dish-limit/{restaurantId}")]
    public async Task<IActionResult> GetRestaurantDishLimit(int restaurantId)
    {
        var restaurant = await _context.Restaurants.FindAsync(restaurantId);
        if (restaurant == null) return NotFound();

        var dishCount = await _context.Dishes.CountAsync(d => d.RestaurantId == restaurantId);
        var maxDishes = restaurant.IsPremium ? -1 : 8; // -1 = unlimited

        return Ok(new
        {
            currentCount = dishCount,
            maxDishes,
            isPremium = restaurant.IsPremium,
            canAddMore = restaurant.IsPremium || dishCount < 8
        });
    }
}
