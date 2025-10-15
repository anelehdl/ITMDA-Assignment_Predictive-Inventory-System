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

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); //cookie expiration          //aligned with jwt token expiration
        options.SlidingExpiration = false;          //removed cookie sliding expiration for better security

        options.Cookie.HttpOnly = true; //mitigate XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        //auto signout if jwt in cookie claims expires
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var tokenClaim = context.Principal?.FindFirst("Token")?.Value;
                if (tokenClaim != null)
                {
                    try
                    {
                        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokenClaim);

                        // Add 1-minute grace period for small time drift
                        if (jwt.ValidTo.AddMinutes(1) <= DateTime.UtcNow)
                        {
                            context.HttpContext.Items["AuthExpired"] = true;
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
                        }
                    }
                    catch (Exception ex)
                    {
                        context.HttpContext.Items["AuthExpired"] = true;
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
                    }
                }
            },
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
builder.Services.AddHttpClient("DummyAPI", client =>
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
app.UseAuthorization();

//Map controller routes for MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();