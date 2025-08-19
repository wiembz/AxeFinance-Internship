namespace TechDashboardAPI.Domain.Entities;

public class ProblemLike
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Problem Problem { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
