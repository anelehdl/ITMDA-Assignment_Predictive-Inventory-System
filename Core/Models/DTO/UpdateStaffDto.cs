
namespace Core.Models.DTO
{
    public class UpdateStaffDto
    {
        public string? Id { get; set; }              // route id on API; form hidden field on Dashboard
        public string? FirstName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? RoleId { get; set; }
        public string? NewPassword { get; set; }    // optional; if provided, updates the password
    }
}
