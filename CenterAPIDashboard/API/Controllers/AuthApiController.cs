using Core.Interfaces;
using Core.Models.DTO;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;


/// <summary>
/// Authentication API controller handles user authentication endpoints.
/// Provides REST API endpoints for login/logout functionality.
/// Returns JWT tokens for successful authentication.
/// </summary>

[ApiController]
[Route("api/auth")] //base route /api/auth
public class AuthApiController : ControllerBase
{
    private readonly IAuthenticationService _authService;           //using the interface instead of the concrete class for better abstraction

    public AuthApiController(IAuthenticationService authService)
    {
        _authService = authService;
    }


    /// <summary>
    ///POST /api/auth/login
    ///authenticates user with email and password,
    ///returns JWT token on success for API calls.
    /// </summary>

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        // ============================================================
        // STEP 1: Validate Input
        // ============================================================
        //ensures email and password are provided
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        // ============================================================
        // STEP 2: Attempt Authentication
        // ============================================================
        //call authentication service to validate credentials
        var result = await _authService.LoginAsync(request.Email, request.Password);

        // ============================================================
        // STEP 3: Handle Authentication Failure
        // ============================================================
        //if login failed, return 401 Unauthorized with message
        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        // ============================================================
        // STEP 4: Return Successful Response
        // ============================================================
        //client should store token for authenticated API requests
        return Ok(result);
    }


    /// <summary>
    ///POST /api/auth/logout
    ///logs out the user
    /// </summary>

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto? request)
    {
        // ============================================================
        // STEP 1: Validate Refresh Token
        // ============================================================
        // Allow logout without refresh token (graceful degradation)
        if (string.IsNullOrEmpty(request?.RefreshToken))
        {
            return Ok(new { message = "Logged out successfully (no token to invalidate)" });
        }

        // ============================================================
        // STEP 2: Invalidate Refresh Token
        // ============================================================
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            // Log the error but return success - user experience is more important
            // than ensuring the token was invalidated
            Console.WriteLine($"Logout error: {ex.Message}");
            return Ok(new { message = "Logged out successfully" });
        }
    }

    //attempting to fix the refresh token endpoint

    /// <summary>
    /// POST /api/auth/refresh
    /// issues a new JWT and refresh token using a valid refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return BadRequest(new { message = "Refresh token required" });

        try
        {
            var (newAccessToken, newRefreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);

            return Ok(new
            {
                Success = true,                  //added this to fix logout issues that was plaguing my brain for the past day
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Message = "Token refreshed successfully"
            });
        }
        catch (SecurityTokenException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Unexpected error: {ex.Message}" });
        }
    }
}