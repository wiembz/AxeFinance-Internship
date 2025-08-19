namespace TechDashboardAPI.Infrastructure.Auth;

public interface IFakeAdService
{
    bool ValidateUser(string email);
    Task<FakeAdUser?> AuthenticateAsync(string email, string password);
    bool IsEnabled { get; }
}

public class FakeAdUser
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
