using TechDashboardAPI.Application.DTOs;
using TechDashboardAPI.Application.Responses;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Application.Interfaces;
public interface IProblemService
{
   
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByProjectAsync(
        int projectId, 
        int pageNumber, 
        int pageSize, 
        string? tags = null, 
        ProblemStatus? status = null, 
        string? priority = null, 
        int? createdBy = null);

    Task<ApiResponse<ProblemDetailDto>> GetProblemByIdAsync(int id, int userId);

    Task<ApiResponse<ProblemResponseDto>> CreateProblemAsync(CreateProblemDto createDto, int userId);

    Task<ApiResponse<ProblemResponseDto>> UpdateProblemAsync(int id, UpdateProblemDto updateDto, int userId);

    
    Task<ApiResponse<bool>> DeleteProblemAsync(int id, int userId);

    
    Task<ApiResponse<bool>> LikeProblemAsync(int problemId, int userId);

   
    Task<ApiResponse<bool>> UnlikeProblemAsync(int problemId, int userId);

   
    Task<ApiResponse<List<string>>> GetAllTagsAsync();

   
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetUserAccessibleProblemsAsync(
        int userId, 
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null);

   
    Task<ApiResponse<PaginatedResponse<ProblemResponseDto>>> GetProblemsByUserAsync(
        int userId, 
        int requestingUserId, 
        int pageNumber, 
        int pageSize);

   
    Task<ApiResponse<ProblemStatisticsDto>> GetProblemStatisticsAsync(int projectId, int userId);

    Task<ApiResponse<bool>> ChangeStatusAsync(
        int problemId, 
        ProblemStatus newStatus, 
        int userId, 
        string? statusChangeComments = null);
}

public class ProblemStatisticsDto
{
   
    public int TotalProblems { get; set; }

    public int OpenProblems { get; set; }

  
    public int InProgressProblems { get; set; }

   
    public int ResolvedProblems { get; set; }

    public double ResolutionRate { get; set; }

  
    public double AverageResolutionTimeInDays { get; set; }

    public Dictionary<string, int> TopTags { get; set; } = new();

    public Dictionary<string, int> MonthlyTrends { get; set; } = new();
}
