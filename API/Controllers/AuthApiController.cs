using Core.Interfaces;
using Core.Models.DTO;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;


/// <summary>
/// Authentication API controller handles user authentication endpoints.
/// Provides REST API endpoints for login/logout functionality.
/// Returns JWT tokens for successful authentication.
/// </summary>

[ApiController]
[Route("api/auth")] //base route /api/auth
public class AuthApiController : ControllerBase
{
    private readonly AuthenticationService _authService;

    public AuthApiController(AuthenticationService authService)
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
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }
}