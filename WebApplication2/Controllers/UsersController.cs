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
using WebApplication2.Models;

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
    public IActionResult GetUsers(string? search, string? role, int page = 1, int pageSize = 10)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search));
        }

        var total = query.Count();
        var now = DateTime.UtcNow;

        // Online count across ALL users (not just current page)
        var onlineCount = _context.Users.Count(u =>
            u.LastActiveAt != null && u.LastActiveAt > now.AddMinutes(-1));

        // Guest online count
        var guestOnlineCount = _context.GuestSessions.Count(g =>
            g.LastActiveAt > now.AddMinutes(-1));

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
                u.CreatedAt,
                u.LastActiveAt,
                IsOnline = u.LastActiveAt != null && u.LastActiveAt > now.AddMinutes(-5)
            })
            .ToList();

        return Ok(new
        {
            total,
            onlineCount,
            guestOnlineCount,
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
            user.UserLevel,
            user.Avatar   // 🔥 thêm dòng này
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
        user.Avatar = dto.Avatar;

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
            user.Avatar,
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
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        // Verify password: support both BCrypt hash and plain text
        bool passwordValid = false;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash);
        }
        catch
        {
            // Fallback for plain text passwords in DB
            passwordValid = user.PasswordHash == request.PasswordHash;
        }

        if (!passwordValid)
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


    // APP
    // POST: api/users/app-login
    [HttpPost("app-login")]
    public IActionResult AppLogin([FromBody] LoginRequest request)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (user == null)
            return Unauthorized("Sai email hoặc mật khẩu");

        // Verify password: support both BCrypt hash and plain text
        bool passwordValid = false;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.PasswordHash, user.PasswordHash);
        }
        catch
        {
            // Fallback for plain text passwords in DB
            passwordValid = user.PasswordHash == request.PasswordHash;
        }

        if (!passwordValid)
            return Unauthorized("Sai email hoặc mật khẩu");

        // ❌ Không cho admin login app
        if (user.Role == "Admin")
            return Unauthorized("Admin đăng nhập ở hệ thống web");

        // ❌ Không cho tài khoản bị khóa
        if (user.Status != "Active")
            return Unauthorized("Tài khoản đã bị khóa");

        // 🔥 CHECK NHÀ HÀNG Ở ĐÂY
        var restaurant = _context.Restaurants
            .FirstOrDefault(r => r.OwnerId == user.UserId);

        // Cập nhật trạng thái hoạt động
        user.LastActiveAt = DateTime.UtcNow;
        _context.SaveChanges();

        // 🔐 JWT
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
            expires: DateTime.Now.AddHours(24),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            )
        );

        // 🔥 TRẢ THÊM hasRestaurant
        return Ok(new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            FullName = user.FullName,
            HasRestaurant = restaurant != null // 🔥 QUAN TRỌNG
        });
    }


    // POST: api/users/register
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        var existingUser = _context.Users
            .FirstOrDefault(u => u.Email == request.Email);

        if (existingUser != null)
            return BadRequest("Email đã tồn tại");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash),
            FullName = request.FullName,
            Role = request.Role ?? "User",
            UserLevel = 0,
            Status = "Active",
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        // 🔥 TẠO TOKEN LUÔN
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token = token,
            role = user.Role,
            fullName = user.FullName
        });
    }

    // 🔥 Hàm tạo JWT dùng chung
    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    [HttpPost("register-restaurant")]
    public IActionResult RegisterRestaurant([FromBody] RegisterRestaurantDto request)
    {
        var user = new User
        {
            FullName = request.RestaurantName,
            Address = request.Address,
            Phone = request.Phone,
            Role = "Restaurant",
            Status = "Active",
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok("Đăng ký thành công");
    }

    // POST: api/users/heartbeat
    [Authorize]
    [HttpPost("heartbeat")]
    public IActionResult Heartbeat([FromBody] HeartbeatRequest? request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var userId = int.Parse(userIdClaim);
        var user = _context.Users.Find(userId);
        if (user == null) return NotFound();

        user.LastActiveAt = DateTime.UtcNow;
        if (request != null)
        {
            if (request.Latitude != 0) user.LastLatitude = request.Latitude;
            if (request.Longitude != 0) user.LastLongitude = request.Longitude;
        }
        _context.SaveChanges();

        return Ok();
    }

    // Thêm API này vào UsersController.cs (Backend)
    [Authorize]
    [HttpPost("upload-avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File không hợp lệ");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = _context.Users.Find(userId);
        if (user == null) return NotFound();

        // Tạo thư mục avatars nếu chưa có
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Lưu file
        var fileName = $"avatar_{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Cập nhật đường dẫn vào DB (lưu relative path, client tự ghép IP)
        user.Avatar = $"/avatars/{fileName}";
        _context.SaveChanges();

        return Ok(new { url = user.Avatar });
    }

    // POST: api/users/guest-heartbeat
    [AllowAnonymous]
    [HttpPost("guest-heartbeat")]
    public IActionResult GuestHeartbeat([FromBody] GuestHeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.DeviceId))
            return BadRequest("DeviceId is required");

        var session = _context.GuestSessions
            .FirstOrDefault(g => g.DeviceId == request.DeviceId);

        if (session == null)
        {
            session = new GuestSession
            {
                DeviceId = request.DeviceId,
                LastActiveAt = DateTime.UtcNow,
                Latitude = request.Latitude != 0 ? request.Latitude : null,
                Longitude = request.Longitude != 0 ? request.Longitude : null
            };
            _context.GuestSessions.Add(session);
        }
        else
        {
                session.LastActiveAt = DateTime.UtcNow;
                    if (request.Latitude != 0) session.Latitude = request.Latitude;
                    if (request.Longitude != 0) session.Longitude = request.Longitude;
                }

                // Also record track point for guest journey
                if (request.Latitude != 0 && request.Longitude != 0)
                {
                    var sessionId = Preferences_GetOrCreateSessionId(request.DeviceId);
                    _context.GuestTrackPoints.Add(new GuestTrackPoint
                    {
                        DeviceId = request.DeviceId,
                        SessionId = sessionId,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        RecordedAt = DateTime.UtcNow
                    });
                }

                _context.SaveChanges();
                return Ok();
            }

            private string Preferences_GetOrCreateSessionId(string deviceId)
            {
                // Use a deterministic session ID based on device + date (new session each day)
                var dateKey = DateTime.UtcNow.ToString("yyyyMMdd");
                return $"{deviceId}_{dateKey}";
            }

    // POST: api/users/logout — clear LastActiveAt so user goes offline immediately
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var userId = int.Parse(userIdClaim);
        var user = _context.Users.Find(userId);
        if (user != null)
        {
            user.LastActiveAt = null;
            _context.SaveChanges();
        }
        return Ok();
    }

    // POST: api/users/guest-logout — clear guest session LastActiveAt
    [AllowAnonymous]
    [HttpPost("guest-logout")]
    public IActionResult GuestLogout([FromBody] GuestHeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.DeviceId))
            return BadRequest();

        var session = _context.GuestSessions
            .FirstOrDefault(g => g.DeviceId == request.DeviceId);
        if (session != null)
        {
            session.LastActiveAt = DateTime.UtcNow.AddMinutes(-10); // mark as offline
            _context.SaveChanges();
        }
        return Ok();
    }

    // GET: api/users/online-details
    [Authorize]
    [HttpGet("online-details")]
    public IActionResult GetOnlineDetails()
    {
        var now = DateTime.UtcNow;

        var onlineUsers = _context.Users
            .Where(u => u.LastActiveAt != null && u.LastActiveAt > now.AddMinutes(-5))
            .Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Role,
                u.LastActiveAt
            })
            .ToList();

        var guestOnlineCount = _context.GuestSessions
            .Count(g => g.LastActiveAt > now.AddMinutes(-5));

        return Ok(new
        {
            onlineUsers,
            guestOnlineCount,
            totalOnline = onlineUsers.Count + guestOnlineCount
        });
    }
}
