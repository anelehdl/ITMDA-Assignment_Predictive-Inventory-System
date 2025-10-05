namespace PrototypeGroupProject_API.Models.DTO
{
    public class StaffLoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }// Plain text password for creation
    }
}
