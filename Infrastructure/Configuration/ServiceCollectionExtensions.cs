using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DummyApp.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions         //this is here to add infrastructure services to declutter Program.cs
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind MongoDBSettings from appsettings.json
            services.Configure<MongoDBSettings>(
                configuration.GetSection("MongoDBSettings"));

            // Register MongoDBContext as singleton
            services.AddSingleton<MongoDBContext>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
                return new MongoDBContext(settings);
            });

            //Registering the business services with a Scoped lifetime
            //Scoped lifetime for web applications where each request gets its own instance 
            //of the service, ensuring that services are not shared across requests (one instance
            //per HTTP request).

            //Update Refactoring to use interfaces for better abstraction and testability and reduce use of concrete classes
            //services.AddScoped<AuthenticationService>(); //handles user login/authentication      //refactored to interface below

            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IRoleService, RoleService>(); //role-based access control        //refactored to interface
            services.AddScoped<IUnifiedUserService, UnifiedUserService>(); //combined client and staff user management (CRUD)            //refactored to interface
            services.AddScoped<IInventoryService, InventoryService>(); //stock metrics and inventory data          //refactored to interface

            return services;
        }
    }
}