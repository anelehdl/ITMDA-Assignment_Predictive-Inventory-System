
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrototypeGroupProject_API.Data;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using PrototypeGroupProject_API.Models.Entities;
using MongoDB.Bson;

namespace PrototypeGroupProject_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<IPasswordHasher<StaffEntity>, PasswordHasher<StaffEntity>>();
            //add db context here
            /*will add this later when integrating the db
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseMongoDB(builder.Configuration.GetConnectionString("URI_CHANGEME"), "MONGODBNAME_CHANGEME");          //going to be the uri that we connect to the mongo db, 2nd paramater is the db name
            });
            */
            //this is for temp testing for api and dashboard communication, using inmem db will switch once db connection has been established
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestingDB");
            });

            //add jwt authentication service
            
            var jwtSection = builder.Configuration.GetSection("JwtSettings");
            var secret = jwtSection.GetValue<string>("Token");      
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!));        //can use ASCII also but for consistency using UTF8, does have a nullable warning but its not the case

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
                        IssuerSigningKey = key,
                        //can add role claim here also      --adding
                        RoleClaimType = ClaimTypes.Role     //explicit mentioning
                        
                    };
                });
            

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();
            //adding a seeded admin user for testing, will remove later
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<StaffEntity>>();

                if (!db.Staff.Any(s => s.Role == "Admin"))
                {
                    var admin = new StaffEntity
                    {
                        Id = ObjectId.GenerateNewId(),      //mongodb id generation
                        Username = "admin",
                        Email = "admin@example.com",
                        Phone = "0000000000",
                        Role = "Admin"
                    };
                    admin.PasswordHash = hasher.HashPassword(admin, "123");
                    db.Staff.Add(admin);
                    db.SaveChanges();
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();        //used for documentation of the api
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            

            app.MapControllers();

            app.Run();
        }
    }
}
