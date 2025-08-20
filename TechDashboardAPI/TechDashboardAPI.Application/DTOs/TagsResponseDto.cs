namespace TechDashboardAPI.Application.DTOs.Problem
{
    public class TagInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }

    public class TagsResponseDto
    {
        public List<TagInfoDto> Tags { get; set; } = new();
        public int TotalTags { get; set; }
        public int TotalProblemsWithTags { get; set; }
        public int MinUsageFilter { get; set; }
        public string MostUsedTag { get; set; } = string.Empty;
        public double AvgTagsPerProblem { get; set; }
    }
}
