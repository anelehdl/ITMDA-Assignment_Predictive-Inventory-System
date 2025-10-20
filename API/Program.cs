using Core.Models;
using Core.Models.DTO;
using DummyApp.Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Net.WebSockets;
using System.Security.Cryptography;
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

// Bind JwtSettings for IOptions<JwtSettings>
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));


// ============================================================
// JWT AUTHENTICATION CONFIGURATION
// ============================================================
//Configuring JWT authentication to secure the API endpoints.
//adding var for "Jwt:Key", "Jwt:Issuer" etc for better practice/clarity

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];     //might need getvalue<string>
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = signingKey
            //think you can add role here also
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
            "https://localhost:5169", 
            "http://localhost:7222",
            "https://localhost:7218",
            "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7222", //dashboard https
            "http://localhost:5169", //dashboard http
            "https://localhost:5169") //dashboard alternative https)
              .AllowAnyHeader()
              .AllowAnyMethod();

        //for mobile app, added .SetIsOriginAllowed to allow any origin
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
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
app.UseCors("AllowMobileApp");

app.UseAuthentication(); //validates JWT tokens
app.UseAuthorization(); //checks user roles/permissions
app.MapControllers(); //routes API requests to controller methods

app.Run();