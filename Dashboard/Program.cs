using Microsoft.AspNetCore.Authentication.Cookies;
using Dashboard.Services;

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
        options.ExpireTimeSpan = TimeSpan.FromHours(24); //cookie expiration
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true; //mitigate XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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