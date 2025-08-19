namespace TechDashboardAPI.Application.Interfaces;

using TechDashboardAPI.Application.DTOs;

public interface IUserService
{
    Task<string> RegisterUserAsync(CreateUserDto dto);
}
