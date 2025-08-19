using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Application.DTOs;

public class UpdateUserDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public int? DepartmentId { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public IFormFile? ProfilePicture { get; set; }
}
