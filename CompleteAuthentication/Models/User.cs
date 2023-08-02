using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CompleteAuthentication.Models
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity), Key()]
        public int Id { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = default!;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = default!;

        public string Email { get; set; } = default!;

        [JsonIgnore]
        public string Password { get; set; } = default!;

        [JsonPropertyName("tfa_secret")]
        public string? TfaSecret { get; set; }


    }
}
