using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Infrastructure.Auth;
using TechDashboardAPI.Infrastructure.Services;
using TechDashboardAPI.Infrastructure.FileStorage;

namespace TechDashboardAPI.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Authentication Services
            services.AddScoped<IFakeAdService, FakeAdService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthService, AuthService>();
            
            // Repository Services
            services.AddScoped<IUserRepository, UserRepository>();
            
            // Business Services
            services.AddScoped<IDepartmentService, DepartmentService>();
            
            // Infrastructure Services
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}