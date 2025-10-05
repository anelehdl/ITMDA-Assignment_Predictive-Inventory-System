namespace PrototypeGroupProject_API.Models.DTO
{
    public class StaffLoginResponseDto
    {

        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        
    }
}
