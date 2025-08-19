using System.ComponentModel.DataAnnotations;

namespace TechDashboardAPI.Application.DTOs;

public class CreateUserDto
{
    [Required]
    public required string Username { get; set; } = default!;

    [Required, EmailAddress]
    public required string Email { get; set; } = default!;

    [Required, MinLength(8)]
    public required string Password { get; set; } = default!;

    public string? Department { get; set; }
    public string? Project { get; set; }
}
