using System.ComponentModel.DataAnnotations;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Domain.Entities
{
    public class Problem
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(5000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Tags { get; set; } = string.Empty; 

        [MaxLength(1000)]
        public string? AzureLink { get; set; }           

        public int? AssignedToUserId { get; set; }       

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        public ProblemStatus Status { get; set; } = ProblemStatus.Requested;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public int LikeCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? AssignedToUser { get; set; }
        public virtual Department Department { get; set; } = null!;
        public virtual Project Project { get; set; } = null!;
        public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();
        public virtual ICollection<ProblemLike> ProblemLikes { get; set; } = new List<ProblemLike>();
        public virtual ICollection<ProblemFieldValue> FieldValues { get; set; } = new List<ProblemFieldValue>();
    }
}
