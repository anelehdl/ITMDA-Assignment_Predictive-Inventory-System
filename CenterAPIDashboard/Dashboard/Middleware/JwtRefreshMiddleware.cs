using Dashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Dashboard.Middleware
{
    public class JwtRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DashboardAuthService authService)
        {
            Console.WriteLine("JwtRefreshMiddleware invoked");        //debug to ensure issue wasnt lying here
            if (context.User.Identity?.IsAuthenticated == true)
            {
                Console.WriteLine("User is authenticated");       //debug
                var tokenClaim = context.User.FindFirst("Token")?.Value;
                // Read refresh token from secure cookie
                var refreshTokenCookie = context.Request.Cookies["refreshToken"];

                if (!string.IsNullOrEmpty(tokenClaim) && !string.IsNullOrEmpty(refreshTokenCookie))
                {
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenClaim);

                    if (jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1))
                    {
                        try
                        {
                            var (newJwt, newRefresh) = await authService.RefreshTokenAsync(refreshTokenCookie);

                            var claims = context.User.Claims
                                .Where(c => c.Type != "Token" && c.Type != "RefreshToken")
                                .ToList();
                            claims.Add(new Claim("Token", newJwt));
                            // No need to add refresh token as claim anymore

                            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTime.UtcNow.AddHours(1)
                            };

                            await context.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            // Update the refresh token cookie
                            context.Response.Cookies.Append(
                                "refreshToken",
                                newRefresh,
                                new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTime.UtcNow.AddDays(7)
                                }
                            );
                        }
                        catch
                        {
                            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            // Remove the refresh token cookie on failure
                            context.Response.Cookies.Delete("refreshToken");
                            // Add a flag to notify user session expired
                            context.Response.Redirect("/Account/Login?reason=expired");
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
