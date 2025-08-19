namespace TechDashboardAPI.Domain.Enums;

/// <summary>
/// Defines the various states a problem can be in throughout its lifecycle.
/// Used to track the progress of problem resolution from creation to completion.
/// </summary>
public enum ProblemStatus
{
    /// <summary>
    /// The problem has been reported but no work has started yet.
    /// This is the initial state for newly created problems.
    /// Team members can propose solutions and like the problem.
    /// </summary>
    Open = 0,

    /// <summary>
    /// The problem is actively being worked on by one or more team members.
    /// Solutions may be in development or testing phase.
    /// Regular updates on progress are expected.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// The problem has been successfully solved and implemented.
    /// A solution has been chosen and deployed.
    /// The problem creator and stakeholders have confirmed the resolution.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// The problem has been officially closed, usually after being resolved.
    /// No further action is expected unless the problem reoccurs.
    /// This is typically the final state for completed problems.
    /// </summary>
    Closed = 3,

    /// <summary>
    /// The problem has been rejected and will not be worked on.
    /// This could be due to it being a duplicate, out of scope,
    /// or not aligned with project goals. Requires justification.
    /// </summary>
    Rejected = 4
}   
