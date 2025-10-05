using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace PrototypeGroupProject_WebDashboard
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();      //gonna use this on the api
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            //add jwt authentication service --not here but in the api -- done  -- leaving code for thought process
            /*
            var jwtSection = builder.Configuration.GetSection("JwtSettings");
            var issuer = jwtSection.GetValue<string>("validIssuer");
            var audience = jwtSection.GetValue<string>("validAudience");
            var secretKey = jwtSection.GetValue<string>("SuperSecretKey");      //can change later for more secure key access
            //var key = System.Text.Encoding.ASCII.GetBytes(secretKey);       //simple key method for now
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        //IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                        //can add role claim here also
                        RoleClaimType = System.Security.Claims.ClaimTypes.Role      //addded role claim type
                    };
                });
            */
            // need to scrap this dashboard doesnt enforce roles, api does
            /*
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireClaim("Role", "Admin"));
            });
            */
            //we are going to use cookies to handle session vars
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/index";
                    options.LogoutPath = "/Auth/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                });


            // Add HttpClient factory
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                // Move base URL to appsettings later (e.g. builder.Configuration["Api:BaseUrl"])
                client.BaseAddress = new Uri("https://localhost:7120"); // adjust to actual API base
                //might need to add default headers here
            });

            // Session support
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(30);     //can adjust as needed, might lower for better security prac?
            });




            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSession();   //needed for current setup before enpoints
            //      not needed for the dashboard, api enforces auth         --adding back for cookies
            app.UseAuthentication();
            app.UseAuthorization();
            
            
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
