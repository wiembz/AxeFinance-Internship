namespace TechDashboardAPI.Domain.Enums;

/// <summary>
/// Defines the review and approval status of solutions proposed for problems.
/// Used to track the lifecycle of solution evaluation and implementation.
/// </summary>
public enum SolutionStatus
{
    /// <summary>
    /// The solution has been submitted and is awaiting review.
    /// This is the initial state for newly proposed solutions.
    /// Reviewers can evaluate the solution and provide feedback.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The solution has been reviewed and approved for implementation.
    /// It is considered a valid and recommended approach to solve the problem.
    /// Implementation can proceed based on this solution.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// The solution has been reviewed and rejected.
    /// It may be incomplete, incorrect, or not aligned with project standards.
    /// Feedback should be provided explaining the rejection reason.
    /// </summary>
    Rejected = 2
}
