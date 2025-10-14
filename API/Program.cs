using DummyApp.Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// SERVICE REGISTRATION
// ============================================================

builder.Services.AddControllers(); //MVC Controllers for  API endppoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Registering the Infrastructure services (MongoDB, business services)
builder.Services.AddInfrastructure(builder.Configuration);


//Registering the business services with a Scoped lifetime
//Scoped lifetime for web applications where each request gets its own instance 
//of the service, ensuring that services are not shared across requests (one instance
//per HTTP request).

builder.Services.AddScoped<AuthenticationService>(); //handles user login/authentication
builder.Services.AddScoped<RoleService>(); //role-based access control
builder.Services.AddScoped<UnifiedUserService>(); //combined client and staff user management (CRUD)
builder.Services.AddScoped<InventoryService>(); //stock metrics and inventory data



// ============================================================
// JWT AUTHENTICATION CONFIGURATION
// ============================================================

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

//Add the Authorization services to enforce role-based access control
builder.Services.AddAuthorization();



// ============================================================
// CORS CONFIGURATION
// ============================================================

//Configured corss-origin resource sharing to allow the Dashboard to call the API,
//which is necessary because API and Dashboard run on different ports.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7222", //dashboard https
            "http://localhost:5169", //dashboard http
            "https://localhost:5169") //dashboard alternative https
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();



// ============================================================
// MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================

//Middleware components are executed in the order they are added, the
//sequence matters.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowDashboard");
app.UseAuthentication(); //validates JWT tokens
app.UseAuthorization(); //checks user roles/permissions
app.MapControllers(); //routes API requests to controller methods

app.Run();