using Core.Models.DTO;

namespace Core.Interfaces
{
    public interface IAuthenticationService
    {
        //login operation
        Task<LoginResponseDto> LoginAsync(string email, string password);

        //adding regresh token and logout operations

        Task<(string Token, string RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);


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