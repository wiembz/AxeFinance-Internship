using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities;

public class ProblemForm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string FormFields { get; set; } = string.Empty; // JSON string of form fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}
