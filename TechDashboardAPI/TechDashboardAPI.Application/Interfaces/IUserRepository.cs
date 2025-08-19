using System.Threading.Tasks;
using TechDashboardAPI.Domain.Entities;

namespace TechDashboardAPI.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserWithDepartmentAsync(int id);
        Task AddUserAsync(User user);
        Task SaveChangesAsync();
    }
}
