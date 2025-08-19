using Microsoft.Extensions.Configuration;

namespace TechDashboardAPI.Infrastructure.Auth
{
    public class FakeAdService : IFakeAdService
    {
        private readonly IConfiguration _configuration;
        
        private static readonly List<FakeAdUser> _adUsers = new()
        {
            new FakeAdUser { Email = "superadmin@company.com", Username = "superadmin", FirstName = "Super", LastName = "Admin", Department = "IT" },
            new FakeAdUser { Email = "admin@company.com", Username = "admin", FirstName = "Admin", LastName = "User", Department = "HR" },
            new FakeAdUser { Email = "viewer@company.com", Username = "viewer", FirstName = "Viewer", LastName = "User", Department = "Finance" },
            new FakeAdUser { Email = "contributor@company.com", Username = "contributor", FirstName = "Contributor", LastName = "User", Department = "IT" }
        };

        public FakeAdService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsEnabled => _configuration["Authentication:FakeAdp:Enabled"] == "true";

        public bool ValidateUser(string email) => IsEnabled && _adUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        public async Task<FakeAdUser?> AuthenticateAsync(string email, string password)
        {
            if (!IsEnabled) return null;
            
            // Simulate async AD lookup
            await Task.Delay(100);
            
            // For demo purposes, accept any password for fake users
            return _adUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        // Backward compatibility method
        public AdUserInfo? GetUserInfo(string email) 
        {
            var fakeUser = _adUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (fakeUser == null) return null;
            
            return new AdUserInfo 
            { 
                Email = fakeUser.Email, 
                Name = $"{fakeUser.FirstName} {fakeUser.LastName}", 
                Department = fakeUser.Department 
            };
        }
    }

    // Keep for backward compatibility
    public class AdUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
