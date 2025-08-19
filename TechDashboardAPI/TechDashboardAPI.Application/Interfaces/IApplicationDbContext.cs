using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Domain.Entities;

namespace TechDashboardAPI.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Department> Departments { get; set; }
    DbSet<Project> Projects { get; set; }
    DbSet<Problem> Problems { get; set; }
    DbSet<Solution> Solutions { get; set; }
    DbSet<ProblemLike> ProblemLikes { get; set; }
    DbSet<ProblemForm> ProblemForms { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
