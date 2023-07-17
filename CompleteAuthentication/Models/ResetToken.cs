using System.ComponentModel.DataAnnotations;

namespace CompleteAuthentication.Models
{
    public class ResetToken
    {
        [Key()]
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}
