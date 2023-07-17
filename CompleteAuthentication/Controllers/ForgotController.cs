using CompleteAuthentication.Data;
using CompleteAuthentication.DTOs;
using CompleteAuthentication.Models;
using CompleteAuthentication.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompleteAuthentication.Controllers
{
    [ApiController]
    [Route("api")]
    public class ForgotController : Controller
    {
        private ApplicationDbContext _dbContext;
        private IConfiguration _configuration;
        public ForgotController(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost("forgot")]
        public IActionResult ForgotPassword(ForgotDto dto)
        {
            ResetToken token = new()
            {
                Email = dto.Email,
                Token = Guid.NewGuid().ToString()
            };
            _dbContext.ResetTokens.Add(token);
            _dbContext.SaveChanges();
            MailService.SendPasswordResetEmailAsync(token);
            return Ok(new
            {
                message = "Reset link emailed"
            });
        }

        [HttpPost("reset")]
        public IActionResult ResetPassword(ResetDto dto)
        {
            if (dto.Password != dto.PasswordConfirm)
            {
                return Unauthorized("Passwords do not match");
            }

            ResetToken? resetToken = _dbContext.ResetTokens.FirstOrDefault(t=>t.Token == dto.Token);
            if (resetToken == null)
            {
                return BadRequest("Invalid Request");
            }

            User? user = _dbContext.Users.FirstOrDefault(u => u.Email == resetToken.Email);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            user.Password = HashService.HashPassword(dto.Password);
            _dbContext.SaveChanges();
            _dbContext.ResetTokens.Remove(_dbContext.ResetTokens.FirstOrDefault(x => x.Email == resetToken.Email));
            _dbContext.SaveChanges();
            return Ok(new
            {
                message = "Password reset successfully"
            });
        }

    }
}
