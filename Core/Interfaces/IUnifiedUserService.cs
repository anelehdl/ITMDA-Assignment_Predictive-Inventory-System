using Core.Models;
using Core.Models.DTO;

namespace Core.Interfaces
{
    public interface IUnifiedUserService
    {
        Task<List<UnifiedUserDto>> GetAllUsersAsync(UserFilterDto? filter = null);
        Task<UnifiedUserDto?> GetUserByIdAsync(string userId, string userType);
        Task<string> CreateStaffUserAsync(CreateStaffDto createStaff);
        Task<string> CreateClientUserAsync(CreateClientDto createClient);
        Task<bool> DeleteUserAsync(string userId, string userType);

        //adding update for staff
        Task<bool> UpdateStaffUserAsync(string userId, UpdateStaffDto updateStaff);


        /*refactoring to make interfaces more focused and reduce concrete class dependencies
        // Get operations
        Task<Staff?> GetUserByIdAsync(string userId);
        Task<Staff?> GetUserByUsernameAsync(string username);
        Task<Staff?> GetUserByEmailAsync(string email);
        Task<IEnumerable<Staff>> GetAllUsersAsync();
        Task<IEnumerable<Staff>> GetUsersByRoleAsync(string roleId);

        // Create/Update/Delete operations
        Task<bool> CreateUserAsync(Staff user);
        Task<bool> UpdateUserAsync(string userId, Staff user);
        Task<bool> DeleteUserAsync(string userId);
        */
    }

}