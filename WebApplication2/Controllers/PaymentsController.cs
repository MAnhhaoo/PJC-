using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApplication2.Data;
using WebApplication2.DTOs;
using System.Security.Claims;


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


}
