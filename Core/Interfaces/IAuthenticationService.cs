using Core.Models;
using Core.Models.DTO;

namespace Core.Interfaces
{
    public interface IAuthenticationService
    {
        //login operation
        Task<LoginResponseDto> LoginAsync(string email, string password);



        /*      Refactoring to make interfaces more focused and reduce concrete class dependencies
        // User authentication
        Task<Authentication?> ValidateUserAsync(string username, string password);
        Task<Authentication?> GetUserByIdAsync(string userId);
        Task<Authentication?> GetUserByUsernameAsync(string username);

        // User management
        Task<bool> CreateUserAsync(Authentication user);
        Task<bool> UpdateUserAsync(string userId, Authentication user);
        Task<bool> DeleteUserAsync(string userId);
        */
    }
}