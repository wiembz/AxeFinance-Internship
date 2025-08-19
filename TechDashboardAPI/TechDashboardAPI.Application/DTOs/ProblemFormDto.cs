namespace TechDashboardAPI.Application.DTOs;

public class CreateProblemFormDto
{
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public List<FormFieldDto> FormFields { get; set; } = new List<FormFieldDto>();
}

public class UpdateProblemFormDto
{
    public string Name { get; set; } = string.Empty;
    public List<FormFieldDto> FormFields { get; set; } = new List<FormFieldDto>();
}

public class ProblemFormResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<FormFieldDto> FormFields { get; set; } = new List<FormFieldDto>();
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class FormFieldDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // text, textarea, select, checkbox, file
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public List<string>? Options { get; set; } = new List<string>(); // For select fields
    public string? DefaultValue { get; set; }
    public int Order { get; set; }
}
