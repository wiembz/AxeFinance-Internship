using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities
{
    public static class ProblemExtensions
    {
        public static bool IsOpen(this Problem p) =>
            p.Status == ProblemStatus.Open || p.Status == ProblemStatus.InProgress;

        public static bool IsResolved(this Problem p) =>
            p.Status == ProblemStatus.Resolved || p.Status == ProblemStatus.Closed;

        public static IEnumerable<string> GetTagList(this Problem p) =>
            (p.Tags ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
