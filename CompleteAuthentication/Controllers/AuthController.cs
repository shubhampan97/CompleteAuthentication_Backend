using CompleteAuthentication.Data;
using CompleteAuthentication.DTOs;
using CompleteAuthentication.Models;
using CompleteAuthentication.Services;
using Google.Apis.Auth;
using Google.Authenticator;
using Microsoft.AspNetCore.Mvc;

namespace CompleteAuthentication.Controllers
{
    [ApiController]
    [Route("api")]
    public class AuthController : Controller
    {
        private ApplicationDbContext _dbContext;
        private IConfiguration _configuration;

        private readonly string appName = "LoginApi";
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

            if(user.TfaSecret is not null)
            {
                return Ok(new
                {
                    id = user.Id,
                    secret = user.TfaSecret
                });
            }

            Random random = new Random();
            string secret = new(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", 32).Select(s => s[random.Next(s.Length)]).ToArray());

            string otpAuthUrl = $"otpauth://totp/{appName}:Secret?secret={secret}&issuer={appName}".Replace(" ", "%20");

            return Ok(new {
                id = user.Id,
                secret,
                otpauth_url = otpAuthUrl
            });



        }

        [HttpPost("two-factor")]
        public IActionResult TwoFactor(TwoFactorDTO dto)
        {
            User? user = _dbContext.Users.FirstOrDefault(u=>u.Id == dto.Id);

            if(user is null)
            {
                return Unauthorized("Invalid credentials");
            }

            string secret = user.TfaSecret is not null ? user.TfaSecret : dto.Secret;

            TwoFactorAuthenticator tfa = new();
            if(!tfa.ValidateTwoFactorPIN(secret, dto.Code, true))
            {
                return Unauthorized("Invalid Credentials"); 
            }

            if(user.TfaSecret is null)
            {
                _dbContext.Users.FirstOrDefault(x => x.Email == user.Email).TfaSecret = dto.Secret;
                _dbContext.SaveChanges();
            }

            string accessToken = TokenService.CreateAccessToken(dto.Id, _configuration.GetSection("JWT:AccessKey").Value);
            string refreshToken = TokenService.CreateRefreshToken(dto.Id, _configuration.GetSection("JWT:RefreshKey").Value);

            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true
            };
            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
            _dbContext.UserTokens.Add(new()
            {
                UserId = dto.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.Now.AddDays(7),
            });
            _dbContext.SaveChanges();
            return Ok(new
            {
                accessToken
            });
        }


        [HttpPost("google-auth")]
        public async Task<IActionResult> GoogleAuth(GoogleAuthDto dto)
        {
            var googleUser = await GoogleJsonWebSignature.ValidateAsync(dto.Token);
            if(googleUser is null)
            {
                return Unauthorized("Unauthenticated");
            }

            User? user = _dbContext.Users.Where(u=>u.Email==googleUser.Email).FirstOrDefault();
            if (user is null)
            {
                user = new()
                {
                    FirstName = googleUser.GivenName,
                    LastName = googleUser.FamilyName,
                    Email = googleUser.Email,
                    Password = dto.Token
                };

                _dbContext.Users.Add(user);
                _dbContext.SaveChanges();

                user.Id = _dbContext.Users.Where(u => u.Email == user.Email).FirstOrDefault()!.Id;
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
