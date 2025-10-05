namespace PrototypeGroupProject_WebDashboard.Models.DTO
{
    public class LoginResponseDto
    {
        public string? Token { get; set; }      //should these be nullable?
        public string? Username { get; set; }
        public string? Role { get; set; }
        public DateTime ExpirationUtc { get; set; }      //optional, can add later if needed
    }
}
