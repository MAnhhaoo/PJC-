//using Microsoft.AspNetCore.Mvc;
//using WebApplication2.Data;
//using WebApplication2.DTOs;

//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;


//[ApiController]
//[Route("api/[controller]")]
//public class UsersController : ControllerBase
//{
//    private readonly AppDbContext _context;
//    private readonly IConfiguration _config;

//    public UsersController(AppDbContext context, IConfiguration config)
//    {
//        _context = context;
//        _config = config;
//    }


//    public UsersController(AppDbContext context)
//    {
//        _context = context;
//    }

//    // 🔹 GET: api/users
//    [HttpGet]
//    public IActionResult GetUsers()
//    {
//        return Ok(_context.Users.ToList());
//    }

//    // 🔹 POST: api/users/login
//    //[HttpPost("login")]
//    //public IActionResult Login(LoginRequest request)
//    //{
//    //    var user = _context.Users.FirstOrDefault(u =>
//    //        u.Email == request.Email &&
//    //        u.PasswordHash == request.Password // demo, CHƯA hash
//    //    );

//    //    if (user == null)
//    //        return Unauthorized("Sai email hoặc mật khẩu");

//    //    return Ok(new LoginResponse
//    //    {
//    //        UserId = user.UserId,
//    //        FullName = user.FullName,
//    //        Role = user.Role
//    //    });
//    //}


//    // POST: api/users/login khi có jwt 
//    [HttpPost("login")]
//    public IActionResult Login(LoginRequest request)
//    {
//        var user = _context.Users.FirstOrDefault(u =>
//            u.Email == request.Email &&
//            u.PasswordHash == request.Password
//        );

//        if (user == null)
//            return Unauthorized("Sai email hoặc mật khẩu");

//        // 🔑 Tạo claims
//        var claims = new[]
//        {
//        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
//        new Claim(ClaimTypes.Role, user.Role),
//        new Claim("email", user.Email)
//    };

//        var key = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
//        );

//        var token = new JwtSecurityToken(
//            issuer: _config["Jwt:Issuer"],
//            audience: _config["Jwt:Audience"],
//            claims: claims,
//            expires: DateTime.Now.AddMinutes(60),
//            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
//        );

//        return Ok(new
//        {
//            token = new JwtSecurityTokenHandler().WriteToken(token),
//            user.UserId,
//            user.FullName,
//            user.Role
//        });
//    }

//}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication2.Data;
using WebApplication2.DTOs;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    // ✅ CHỈ GIỮ 1 CONSTRUCTOR
    public UsersController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // GET: api/users
    [Authorize]
    [HttpGet]
    public IActionResult GetUsers()
    {
        return Ok(_context.Users.ToList());
    }




    //api/Users/me
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var email = User.FindFirstValue("email");
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { userId, email, role });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        //return Ok("Chỉ admin mới vô được");
        return Ok(_context.Users.ToList());
    }


    // POST: api/users/login
    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u =>
            u.Email == request.Email &&
            u.PasswordHash == request.Password
        );

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user.UserId,
            user.FullName,
            user.Role
        });



    }
}
