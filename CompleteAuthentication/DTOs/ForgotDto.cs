using System.Text.Json.Serialization;

namespace CompleteAuthentication.DTOs
{
    public class ForgotDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;
    }
}
