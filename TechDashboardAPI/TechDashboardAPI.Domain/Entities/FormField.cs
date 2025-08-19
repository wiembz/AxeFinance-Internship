using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities;

public class FormField
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public string Placeholder { get; set; } = string.Empty;
    public string Options { get; set; } = string.Empty; // JSON array for select/radio options
    public int Order { get; set; }
    public string ValidationRules { get; set; } = string.Empty; // JSON object for validation
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;
}
