using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechDashboardAPI.Application.DTOs;

public class CreateSolutionDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int ProblemId { get; set; }

    public List<IFormFile>? Attachments { get; set; }

    [StringLength(1000)]
    public string? Implementation { get; set; }

    public List<string>? Tags { get; set; }
}

public class UpdateSolutionDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Implementation { get; set; }

    public List<string>? Tags { get; set; }

    public List<IFormFile>? NewAttachments { get; set; }

    public List<string>? AttachmentsToRemove { get; set; }
}
