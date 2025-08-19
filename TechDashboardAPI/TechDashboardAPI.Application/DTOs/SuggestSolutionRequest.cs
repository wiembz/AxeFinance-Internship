namespace TechDashboardAPI.Application.DTOs;

public class SuggestSolutionRequest
{
    public int ProblemId { get; set; }
    public string Content { get; set; } = string.Empty;
}
