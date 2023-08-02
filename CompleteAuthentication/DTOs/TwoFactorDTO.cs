namespace CompleteAuthentication.DTOs
{
    public class TwoFactorDTO
    {
        public int Id { get; set; }
        public string Secret { get; set; } = default!;
        public string Code { get; set; } = default!;
    }
}
