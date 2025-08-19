using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Infrastructure.Data;

namespace TechDashboardAPI.Infrastructure.Auth
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Implement GetUserWithDepartmentAsync if needed for AuthService compatibility
        public async Task<User?> GetUserWithDepartmentAsync(int id)
        {
            return await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
