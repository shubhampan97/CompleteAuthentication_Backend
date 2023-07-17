using CompleteAuthentication.Data;
using CompleteAuthentication.DTOs;
using CompleteAuthentication.Models;
using CompleteAuthentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompleteAuthentication.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : Controller
    {
        private ApplicationDbContext _dbContext;
        private IConfiguration _configuration;
        public AuthController(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            if(dto.Password != dto.PasswordConfirm)
            {
                return Unauthorized("Passwords do not match");
            }

            User user = new()
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = HashService.HashPassword(dto.Password)
            };

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            return Ok(user);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto loginDto)
        {
            User user = _dbContext.Users.FirstOrDefault(u => u.Email == loginDto.Email);
            if(user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            if(HashService.HashPassword(loginDto.Password) != user.Password)
            {
                return Unauthorized("Invalid credentials");
            }

            string accessToken = TokenService.CreateAccessToken(user.Id, _configuration.GetSection("JWT:AccessKey").Value);
            string refreshToken = TokenService.CreateRefreshToken(user.Id, _configuration.GetSection("JWT:RefreshKey").Value);

            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true
            };
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
            _dbContext.UserTokens.Add(new()
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.Now.AddDays(7),
            });
            _dbContext.SaveChanges();
            return Ok(new
            {
                accessToken
            });
        }

        [HttpGet("user")]
        public new IActionResult User()
        {
            string authorizationHeader = Request.Headers["Authorization"];
            if (authorizationHeader is null || authorizationHeader.Length <= 8)
            {
                return Unauthorized("Unauthenticated!");
            }
            string accessToken = authorizationHeader[7..];
            int id = TokenService.DecodeToken(accessToken, out bool hasTokenExpired);
            if(hasTokenExpired)
            {
                return Unauthorized("Unauthorized!");
            }
            User? user = _dbContext.Users.FirstOrDefault(x=>x.Id == id);
            if(user == null)
            {
                return Unauthorized("Unauthorized!");
            }

            return Ok(user);
        }

        [HttpPost("refresh")]
        public IActionResult Refresh()
        {
            if (Request.Cookies["refresh_token"] is null)
            {
                return Unauthorized("Unauthenticated");
            }

            string? refreshToken = Request.Cookies["refresh_token"];
            int id = TokenService.DecodeToken(refreshToken, out bool hasTokenExpired);

            if(!_dbContext.UserTokens.Any(x=>x.UserId==id && x.Token == refreshToken && x.ExpiresAt > DateTime.Now))
            {
                return Unauthorized("Unauthenticated");
            }
            if (hasTokenExpired)
            {
                return Unauthorized("Unauthenticated");
            }

            string accessToken = TokenService.CreateAccessToken(id, _configuration.GetSection("JWT:AccessKey").Value);
            return Ok(new
            {
                token = accessToken
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            string? refreshToken = Request.Cookies["refresh_token"];
            if(refreshToken is null)
            {
                return Ok("Already logged out");
            }
            _dbContext.UserTokens.Remove(_dbContext.UserTokens.FirstOrDefault(x => x.Token == refreshToken));
            _dbContext.SaveChanges();
            Response.Cookies.Delete("refresh_token");
            return Ok( new
            {
                message = "Logged out successfully"
            });
        }
    }
}
