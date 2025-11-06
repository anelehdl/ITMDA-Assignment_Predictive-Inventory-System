using Dashboard.Middleware;
using Dashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICE REGISTRATION
// ============================================================

//Add MVC support for web dashboard with views
builder.Services.AddControllersWithViews();

//Register custom authentication service for Dashboard
//Service communicates with API to authenticate users
builder.Services.AddScoped<DashboardAuthService>();



// ============================================================
// COOKIE-BASED AUTHENTICATION CONFIGURATION
// ============================================================

//Configure cookie authentication for Dashboard (web session management),
//NOTE: API uses JWT tokens, Dashboard uses cookies for browser sessions

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Dashboard/Index"; //redirect to login page if not authenticated
        options.LogoutPath = "/Dashboard/Logout";
        options.AccessDeniedPath = "/Dashboard/AccessDenied"; //redirect if user lacks permissions

        options.ExpireTimeSpan = TimeSpan.FromHours(1); //cookie expiration          //changed for dashboard session
        options.SlidingExpiration = false;          

        options.Cookie.HttpOnly = true; //mitigate XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        //auto signout if jwt in cookie claims expires
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var tokenClaim = context.Principal?.FindFirst("Token")?.Value;
                var refreshToken = context.Request.Cookies["refreshToken"];

                if (!string.IsNullOrEmpty(tokenClaim) && !string.IsNullOrEmpty(refreshToken))
                {
                    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenClaim);

                    // Only try refresh if token is expired
                    if (jwt.ValidTo <= DateTime.UtcNow.AddMinutes(2))       //added 2 min graceperiod
                    {
                        var authService = context.HttpContext.RequestServices.GetRequiredService<DashboardAuthService>();

                        try
                        {
                            var (newToken, newRefresh) = await authService.RefreshTokenAsync(refreshToken);

                            // Replace token in claims
                            var claims = context.Principal.Claims
                                .Where(c => c.Type != "Token")
                                .ToList();
                            claims.Add(new Claim("Token", newToken));

                            var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                            context.ReplacePrincipal(new ClaimsPrincipal(newIdentity));
                            context.ShouldRenew = true;

                            // Update refresh cookie
                            context.HttpContext.Response.Cookies.Append("refreshToken", newRefresh, new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.Strict,
                                Expires = DateTime.UtcNow.AddDays(7)
                            });
                        }
                        catch
                        {
                            // Refresh failed logout
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.HttpContext.Response.Cookies.Delete("refreshToken");
                        }
                    }
                }
        }
            ,
            OnRedirectToLogin = context =>
            {
                var isApi = context.Request.Path.StartsWithSegments("/api");
                if (isApi)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                var reason = context.HttpContext.Items.ContainsKey("AuthExpired") ? "expired" : "unauthorized";
                var redirectUrl = QueryHelpers.AddQueryString(context.RedirectUri, "reason", reason);
                context.Response.Redirect(redirectUrl);
                return Task.CompletedTask;
            }
        };
    });



// ============================================================
// SERVICE REGISTRATION
// ============================================================

//HttpClient for communicating with the API
//Allows Dashboard controller to make API calls
builder.Services.AddHttpClient("CentralAPIDashboard", client =>
{
    //set base URL of the API
    client.BaseAddress = new Uri("https://localhost:7218");
});

var app = builder.Build();



// ============================================================
// MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================

//Shows detailed error information for debugging
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); //redirect to error page
    app.UseHsts(); //enable HTTP Strict Transport Security
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<JwtRefreshMiddleware>();  // <-- the custom middleware to try to fix the refresh token issue
app.UseAuthorization();

//Map controller routes for MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();