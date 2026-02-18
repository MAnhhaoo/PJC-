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



//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.IdentityModel.Tokens;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;
//using WebApplication2.Data;
//using WebApplication2.DTOs;

//[ApiController]
//[Route("api/[controller]")]
//public class UsersController : ControllerBase
//{
//    private readonly AppDbContext _context;
//    private readonly IConfiguration _config;

//    // ✅ CHỈ GIỮ 1 CONSTRUCTOR
//    public UsersController(AppDbContext context, IConfiguration config)
//    {
//        _context = context;
//        _config = config;
//    }

//    // GET: api/users
//    [Authorize]
//    [HttpGet]
//    public IActionResult GetUsers()
//    {
//        return Ok(_context.Users.ToList());
//    }




//    //api/Users/me
//    [Authorize]
//    [HttpGet("me")]
//    public IActionResult Me()
//    {
//        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
//                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

//        var email = User.FindFirstValue(ClaimTypes.Email);
//        var role = User.FindFirstValue(ClaimTypes.Role);

//        return Ok(new { userId, email, role });
//    }

//    [Authorize(Roles = "Admin")]
//    [HttpGet("admin-only")]
//    public IActionResult AdminOnly()
//    {
//        //return Ok("Chỉ admin mới vô được");
//        return Ok(_context.Users.ToList());
//    }


//    // POST: api/users/login
//    [HttpPost("login")]
//    public IActionResult Login(LoginRequest request)
//    {
//        var user = _context.Users.FirstOrDefault(u =>
//            u.Email == request.Email &&
//            u.PasswordHash == request.Password
//        );

//        if (user == null)
//            return Unauthorized("Sai email hoặc mật khẩu");

//        var claims = new[]
//        {
//            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
//            new Claim(ClaimTypes.Role, user.Role),
//            new Claim(ClaimTypes.Email, user.Email)
//        };

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

    public UsersController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // GET: api/users
    [Authorize]
    [HttpGet]
    public IActionResult GetUsers(string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search));
        }

        var total = query.Count();

        var users = query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.Role,
                u.UserLevel,
                u.Status,
                u.CreatedAt
            })
            .ToList();

        return Ok(new
        {
            total,
            users
        });
    }




    // GET: api/users/me
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var userId = int.Parse(userIdClaim);

        var user = _context.Users.Find(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Email,
            user.FullName,
            user.Phone,
            user.Address,
            user.Role,
            user.UserLevel
        });
    }

    // GET: api/users/update 
    [Authorize]
    [HttpPut("me")]
    public IActionResult UpdateMe([FromBody] UpdateProfileDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var userId = int.Parse(userIdClaim);
        var role = User.FindFirstValue(ClaimTypes.Role);

        var user = _context.Users.Find(userId);
        if (user == null)
            return NotFound("User không tồn tại");

        // ✅ user nào cũng update được
        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.Address = dto.Address;

        // 🔐 chỉ admin mới sửa được UserLevel
        if (role == "Admin" && dto.UserLevel.HasValue)
        {
            user.UserLevel = dto.UserLevel.Value;
        }

        _context.SaveChanges();

        return Ok(new
        {
            message = "Cập nhật thành công",
            user.FullName,
            user.Phone,
            user.Address,
            user.UserLevel
        });
    }


    // GET: api/users/admin-only
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly()
    {
        return Ok(_context.Users.ToList());
    }

    // POST: api/users/login
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u =>
            u.Email == request.Email &&
            u.PasswordHash == request.PasswordHash
        );

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        // 🔐 Chỉ Admin được login
        if (user.Role != "Admin")
            return Unauthorized("Bạn không có quyền đăng nhập hệ thống này");

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            )
        );

        return Ok(new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            FullName = user.FullName
        });
    }



    // PUT: api/users/{id}/toggle-status
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/toggle-status")]
    public IActionResult ToggleStatus(int id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            return NotFound();

        // ❌ Không cho lock Admin
        if (user.Role == "Admin")
            return BadRequest("Không thể khóa tài khoản Admin");

        user.Status = user.Status == "Active" ? "Banned" : "Active";

        _context.SaveChanges();

        return Ok(new { user.Status });
    }





}
