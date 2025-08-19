using System.ComponentModel.DataAnnotations;

namespace TechDashboardAPI.Domain.Entities;

/// <summary>
/// Represents an organizational department that contains projects and manages resources.
/// Departments are the top-level organizational unit in the system hierarchy.
/// </summary>
public class Department
{
    public int Id { get; set; }
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public int? DepartmentHeadId { get; set; }
    [MaxLength(10)]
    public string? DepartmentCode { get; set; }
    [MaxLength(200)]
    public string? Location { get; set; }
    [MaxLength(100)]
    [EmailAddress]
    public string? ContactEmail { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual User? DepartmentHead { get; set; }
    public int GetTotalProjectsCount() => Projects.Count;
    public int GetTotalProblemsCount() => Projects.SelectMany(p => p.Problems).Count();
    public int GetAgeInDays() => (DateTime.UtcNow - CreatedDate).Days;
    public void AssignDepartmentHead(int departmentHeadUserId)
    {
        DepartmentHeadId = departmentHeadUserId;
        LastUpdatedDate = DateTime.UtcNow;
    }
    public void UpdateContactInfo(string? contactEmail, string? location)
    {
        ContactEmail = contactEmail;
        Location = location;
        LastUpdatedDate = DateTime.UtcNow;
    }
    public string GetPerformanceSummary()
    {
        var totalProjects = GetTotalProjectsCount();
        var totalProblems = GetTotalProblemsCount();
        return $"Department: {Name} | Projects: {totalProjects} total | Problems: {totalProblems} total";
    }
}
