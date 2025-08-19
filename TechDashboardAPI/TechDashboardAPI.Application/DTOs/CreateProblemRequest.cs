using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechDashboardAPI.Application.DTOs;

public class CreateProblemRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters.")]
    public string? Tags { get; set; }

    [Required(ErrorMessage = "Project ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Project ID must be a valid positive integer.")]
    public int ProjectId { get; set; }

    public IFormFile? Attachment { get; set; }
}
