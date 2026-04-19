namespace YourProjectName.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; }
    }
}