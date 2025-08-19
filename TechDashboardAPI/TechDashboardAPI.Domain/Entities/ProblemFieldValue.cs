namespace TechDashboardAPI.Domain.Entities;

public class ProblemFieldValue
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int FormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Problem Problem { get; set; } = null!;
    public virtual FormField FormField { get; set; } = null!;
}
