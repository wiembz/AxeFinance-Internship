using Microsoft.EntityFrameworkCore;
using TechDashboardAPI.Application.Interfaces;
using TechDashboardAPI.Domain.Entities;
using TechDashboardAPI.Domain.Enums;

namespace TechDashboardAPI.Infrastructure.Data;


public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    #region Entity Sets

    public DbSet<User> Users { get; set; }


    public DbSet<Department> Departments { get; set; }


    public DbSet<Project> Projects { get; set; }


    public DbSet<Problem> Problems { get; set; }


    public DbSet<Solution> Solutions { get; set; }


    public DbSet<ProblemLike> ProblemLikes { get; set; }


    public DbSet<ProblemForm> ProblemForms { get; set; }

    public DbSet<FormField> FormFields { get; set; }

    public DbSet<ProblemFieldValue> ProblemFieldValues { get; set; }
    #endregion

public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) 
    { 
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Production performance optimizations - disable expensive logging features
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableDetailedErrors(false);
            optionsBuilder.EnableServiceProviderCaching(true);
            
            // Configure query behavior for better performance
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Default(WarningBehavior.Log));
            
            // For development debugging, you can enable these temporarily:
            // optionsBuilder.EnableSensitiveDataLogging(true);
            // optionsBuilder.EnableDetailedErrors(true);
        }
    }

    /// <summary>
    /// Configures entity relationships, constraints, indexes, and database schema.
    /// Organizes configuration into separate methods for maintainability.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entities using separate methods for better organization and maintainability
        ConfigureUserEntity(modelBuilder);
        ConfigureDepartmentEntity(modelBuilder);
        ConfigureProjectEntity(modelBuilder);
        ConfigureProblemEntity(modelBuilder);
        ConfigureSolutionEntity(modelBuilder);
        ConfigureProblemLikeEntity(modelBuilder);
        ConfigureFormEntities(modelBuilder);
    }

    /// <summary>
    /// Saves changes to the database with automatic audit trail updates.
    /// Updates LastModifiedDate properties and handles concurrency conflicts gracefully.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();
        
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts gracefully
            throw new InvalidOperationException(
                "The record you attempted to edit was modified by another user after you got the original value. " +
                "The edit operation was canceled and the current values in the database have been displayed. " +
                "If you still want to edit this record, click the Edit button again.",
                ex);
        }
    }

    /// <summary>
    /// Updates audit fields (like LastModifiedDate) on entities before saving.
    /// Automatically tracks when entities are created or modified for audit purposes.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            // Handle entities with LastModifiedDate property
            if (entry.Entity is User user && entry.State == EntityState.Modified)
            {
                user.LastModifiedDate = utcNow;
            }
            else if (entry.Entity is Problem problem && entry.State == EntityState.Modified)
            {
                problem.LastUpdatedDate = utcNow;
            }
            else if (entry.Entity is Solution solution && entry.State == EntityState.Modified)
            {
                solution.LastModifiedDate = utcNow;
            }
            else if (entry.Entity is Project project && entry.State == EntityState.Modified)
            {
                project.LastUpdatedDate = utcNow;
            }
            else if (entry.Entity is Department department && entry.State == EntityState.Modified)
            {
                department.LastUpdatedDate = utcNow;
            }
        }
    }

    #region Entity Configuration Methods
    
    /// <summary>
    /// Configures the User entity with advanced constraints, indexes, and relationships.
    /// Implements security features like password hashing requirements and role-based access control.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("Users");
            
            // Configure string properties with appropriate lengths and constraints
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false); // ASCII only for usernames - better performance
                
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
                
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),
                    v => (UserRole)Enum.Parse(typeof(UserRole), v))
                .HasMaxLength(20);
                
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);
                
            // Configure timestamp properties
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastLoginDate)
                .IsRequired(false); // Can be null if user never logged in
            
            // Performance-optimized indexes for common queries
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
                
            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");
                
            entity.HasIndex(e => e.Role)
                .HasDatabaseName("IX_Users_Role");
                
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Users_IsActive");
                
            entity.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("IX_Users_DepartmentId");
                
            entity.HasIndex(e => new { e.IsActive, e.Role })
                .HasDatabaseName("IX_Users_IsActive_Role"); // Composite index for admin queries
                
                            // Configure relationships with appropriate cascade behaviors
            entity.HasOne(e => e.Department)
                .WithMany() // Department doesn't have Users collection
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deleting departments with users
                
            entity.HasMany(e => e.Problems)
                .WithOne(p => p.CreatedByUser)
                .HasForeignKey(p => p.CreatedBy) // Use CreatedBy, not CreatedByUserId
                .OnDelete(DeleteBehavior.Restrict); // Keep problems even if user is deleted
                
            entity.HasMany(e => e.Solutions)
                .WithOne(s => s.User) // Use User property, not CreatedByUser
                .HasForeignKey(s => s.UserId) // Use UserId, not CreatedByUserId
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasMany(e => e.ProblemLikes)
                .WithOne(pl => pl.User)
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Remove likes if user is deleted
                
            // Configure User-Project relationship (User belongs to a Project)
            entity.HasOne(e => e.Project)
                .WithMany() // Project doesn't have a collection of Users
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.SetNull); // Set ProjectId to null if project is deleted
        });
    }

    /// <summary>
    /// Configures the Department entity with organizational hierarchy support and audit tracking.
    /// Implements business rules for department management and project relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureDepartmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("Departments");
            
            // Configure properties with business-appropriate constraints
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200); // Match domain entity MaxLength
                
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000); // Match domain entity MaxLength
                
            entity.Property(e => e.DepartmentCode)
                .HasMaxLength(10);
                
            entity.Property(e => e.Location)
                .HasMaxLength(200);
                
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(100);
                
                
            // Configure audit fields
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastUpdatedDate)
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for common operations
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("IX_Departments_Name");
                
            entity.HasIndex(e => e.DepartmentCode)
                .IsUnique()
                .HasDatabaseName("IX_Departments_DepartmentCode")
                .HasFilter("[DepartmentCode] IS NOT NULL"); // Partial index for non-null values
                
                
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_Departments_CreatedBy");
                
            entity.HasIndex(e => e.DepartmentHeadId)
                .HasDatabaseName("IX_Departments_DepartmentHeadId");
            
            // Configure relationships
            entity.HasOne(e => e.CreatedByUser)
                .WithMany() // User doesn't have DepartmentsCreated collection
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Don't allow deleting user who created departments
                
            entity.HasOne(e => e.DepartmentHead)
                .WithMany() // User doesn't have DepartmentsManaged collection
                .HasForeignKey(e => e.DepartmentHeadId)
                .OnDelete(DeleteBehavior.SetNull); // Allow removing department head
                
            entity.HasMany(e => e.Projects)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade); // Remove projects when department is deleted (business decision)
        });
    }

    /// <summary>
    /// Configures the Project entity with advanced project management features and timeline tracking.
    /// Implements project lifecycle management and team collaboration support.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureProjectEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("Projects");
            
            // Configure string properties with appropriate business constraints
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200); // Match domain entity MaxLength
                
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000); // Match domain entity MaxLength
                
            entity.Property(e => e.Tags)
                .HasMaxLength(500); // Match domain entity MaxLength
                
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            // Configure date properties for project timeline management
            entity.Property(e => e.StartDate)
                .IsRequired(false); // Can be null for planning phase projects
                
            entity.Property(e => e.EndDate)
                .IsRequired(false); // Can be null for ongoing projects
                
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastUpdatedDate)
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance-optimized indexes for project management queries
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_Projects_Name");
                
            entity.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("IX_Projects_DepartmentId");
                
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_Projects_CreatedBy");
                
            entity.HasIndex(e => e.ProjectManagerId)
                .HasDatabaseName("IX_Projects_ProjectManagerId");
                
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Projects_IsActive");
                
            entity.HasIndex(e => new { e.DepartmentId, e.IsActive })
                .HasDatabaseName("IX_Projects_Department_IsActive"); // For department dashboard queries
                
            entity.HasIndex(e => new { e.StartDate, e.EndDate })
                .HasDatabaseName("IX_Projects_DateRange") // For timeline queries
                .HasFilter("[StartDate] IS NOT NULL AND [EndDate] IS NOT NULL");
            
            // Configure relationships
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Projects)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade); // Match existing migration behavior
                
            entity.HasOne(e => e.CreatedByUser)
                .WithMany() // User doesn't have ProjectsCreated collection
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Keep projects even if creator is deleted
                
            // Configure Project-ProjectManager relationship (Project has a ProjectManager)
            entity.HasOne(e => e.ProjectManager)
                .WithMany() // User doesn't have a collection of ManagedProjects
                .HasForeignKey(e => e.ProjectManagerId)
                .OnDelete(DeleteBehavior.SetNull); // Set ProjectManagerId to null if manager is deleted
                
            entity.HasMany(e => e.Problems)
                .WithOne(p => p.Project)
                .HasForeignKey(p => p.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove problems when project is deleted
                
            entity.HasMany(e => e.ProblemForms)
                .WithOne(pf => pf.Project)
                .HasForeignKey(pf => pf.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove forms when project is deleted
                
            entity.HasMany(e => e.FormFields)
                .WithOne(ff => ff.Project)
                .HasForeignKey(ff => ff.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove form fields when project is deleted
        });
    }

    /// <summary>
    /// Configures the Problem entity with comprehensive problem tracking and management features.
    /// Implements advanced search capabilities, status tracking, and user interaction support.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureProblemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Problem>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("Problems");
            
            // Configure string properties with appropriate business constraints
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200); // Match domain entity MaxLength
                
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000); // Match domain entity MaxLength
                
            entity.Property(e => e.Tags)
                .HasMaxLength(500); // Match domain entity MaxLength
                
            entity.Property(e => e.AttachmentPath)
                .HasMaxLength(500);
                
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(15);
                
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            // Configure audit and tracking fields
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastUpdatedDate)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LikeCount)
                .HasDefaultValue(0); // Initialize like count to zero
            
            // Performance-optimized indexes for problem queries
            entity.HasIndex(e => e.Title)
                .HasDatabaseName("IX_Problems_Title");
                
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_Problems_ProjectId");
                
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_Problems_CreatedBy");
                
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Problems_Status");
                
            entity.HasIndex(e => e.CreatedDate)
                .HasDatabaseName("IX_Problems_CreatedDate");
                
            entity.HasIndex(e => e.LikeCount)
                .HasDatabaseName("IX_Problems_LikeCount"); // For popular problems queries
                
            entity.HasIndex(e => new { e.ProjectId, e.Status })
                .HasDatabaseName("IX_Problems_Project_Status"); // For project dashboard queries
                
            entity.HasIndex(e => new { e.IsActive, e.Status })
                .HasDatabaseName("IX_Problems_IsActive_Status"); // For active problems filtering
            
            // Configure relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Problems)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove problems when project is deleted
                
            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.Problems)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Keep problems even if creator is deleted
                
            entity.HasMany(e => e.Solutions)
                .WithOne(s => s.Problem)
                .HasForeignKey(s => s.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove solutions when problem is deleted
                
            entity.HasMany(e => e.ProblemLikes)
                .WithOne(pl => pl.Problem)
                .HasForeignKey(pl => pl.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove likes when problem is deleted
                
            entity.HasMany(e => e.FieldValues)
                .WithOne(fv => fv.Problem)
                .HasForeignKey(fv => fv.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove field values when problem is deleted
        });
    }

    /// <summary>
    /// Configures the Solution entity with approval workflow and attachment support.
    /// Implements solution quality control and user contribution tracking.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureSolutionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Solution>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("Solutions");
            
            // Configure string properties with appropriate business constraints
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(5000); // Allow detailed solutions
                
            entity.Property(e => e.AttachmentPath)
                .HasMaxLength(500);
                
            entity.Property(e => e.AzureDevOpsLink)
                .HasMaxLength(500);
                
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(15);
                
            // Configure audit and approval fields
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.LastModifiedDate)
                .HasDefaultValueSql("GETUTCDATE()");
                
            entity.Property(e => e.ApprovedDate)
                .IsRequired(false); // Can be null if not yet approved
            
            // Performance-optimized indexes for solution queries
            entity.HasIndex(e => e.ProblemId)
                .HasDatabaseName("IX_Solutions_ProblemId");
                
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Solutions_UserId");
                
            entity.HasIndex(e => e.ApprovedBy)
                .HasDatabaseName("IX_Solutions_ApprovedBy");
                
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Solutions_Status");
                
            entity.HasIndex(e => e.CreatedDate)
                .HasDatabaseName("IX_Solutions_CreatedDate");
                
            entity.HasIndex(e => e.ApprovedDate)
                .HasDatabaseName("IX_Solutions_ApprovedDate")
                .HasFilter("[ApprovedDate] IS NOT NULL"); // Partial index for approved solutions
                
            entity.HasIndex(e => new { e.ProblemId, e.Status })
                .HasDatabaseName("IX_Solutions_Problem_Status"); // For problem solution listing
                
            entity.HasIndex(e => new { e.UserId, e.Status })
                .HasDatabaseName("IX_Solutions_User_Status"); // For user contribution tracking
            
            // Configure relationships
            entity.HasOne(e => e.Problem)
                .WithMany(p => p.Solutions)
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove solutions when problem is deleted
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.Solutions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Keep solutions even if creator is deleted
                
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany() // User doesn't have ApprovedSolutions collection
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull); // Remove approval if approver is deleted
        });
    }

    /// <summary>
    /// Configures the ProblemLike entity with user interaction tracking.
    /// Implements problem popularity and user engagement features.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    private void ConfigureProblemLikeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProblemLike>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("ProblemLikes");
            
            // Configure audit fields
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance-optimized indexes for like queries
            entity.HasIndex(e => e.ProblemId)
                .HasDatabaseName("IX_ProblemLikes_ProblemId");
                
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_ProblemLikes_UserId");
                
            entity.HasIndex(e => new { e.ProblemId, e.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ProblemLikes_Problem_User"); // Prevent duplicate likes
                
            entity.HasIndex(e => e.CreatedDate)
                .HasDatabaseName("IX_ProblemLikes_CreatedDate"); // For trending analysis
            
            // Configure relationships
            entity.HasOne(e => e.Problem)
                .WithMany(p => p.ProblemLikes)
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove likes when problem is deleted
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.ProblemLikes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Keep likes even if user is deleted for analytics
        });
    }

    /// <summary>
    /// Configures the dynamic form entities (ProblemForm, FormField, ProblemFieldValue) for flexible problem submission.
    /// Implements customizable form building and data collection capabilities.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entities.</param>
    private void ConfigureFormEntities(ModelBuilder modelBuilder)
    {
        // Configure ProblemForm entity for dynamic form definitions
        modelBuilder.Entity<ProblemForm>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("ProblemForms");
            
            // Configure string properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200); // Allow descriptive form names
                
            entity.Property(e => e.FormFields)
                .HasMaxLength(4000); // JSON field definitions
                
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            // Configure audit fields
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for form management
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_ProblemForms_Name");
                
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_ProblemForms_ProjectId");
                
            entity.HasIndex(e => e.CreatedBy)
                .HasDatabaseName("IX_ProblemForms_CreatedBy");
                
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_ProblemForms_IsActive");
                
            entity.HasIndex(e => new { e.ProjectId, e.IsActive })
                .HasDatabaseName("IX_ProblemForms_Project_IsActive"); // For active forms in project
            
            // Configure relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProblemForms)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove forms when project is deleted
                
            entity.HasOne(e => e.CreatedByUser)
                .WithMany() // User doesn't have FormsCreated collection
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Keep forms even if creator is deleted
        });

        // Configure FormField entity for individual form field definitions
        modelBuilder.Entity<FormField>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("FormFields");
            
            // Configure string properties
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Label)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
                
            entity.Property(e => e.Placeholder)
                .HasMaxLength(200);
                
            entity.Property(e => e.Options)
                .HasMaxLength(2000); // JSON options for dropdowns, etc.
                
            entity.Property(e => e.ValidationRules)
                .HasMaxLength(1000); // JSON validation rules
                
            entity.Property(e => e.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);
                
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            entity.Property(e => e.Order)
                .HasDefaultValue(0);
                
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for form field queries
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_FormFields_Name");
                
            entity.HasIndex(e => e.ProjectId)
                .HasDatabaseName("IX_FormFields_ProjectId");
                
            entity.HasIndex(e => e.Type)
                .HasDatabaseName("IX_FormFields_Type");
                
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_FormFields_IsActive");
                
            entity.HasIndex(e => new { e.ProjectId, e.Order })
                .HasDatabaseName("IX_FormFields_Project_Order"); // For ordered field display
                
            entity.HasIndex(e => new { e.ProjectId, e.IsActive })
                .HasDatabaseName("IX_FormFields_Project_IsActive"); // For active fields in project
            
            // Configure relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.FormFields)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Remove fields when project is deleted
        });

        // Configure ProblemFieldValue entity for storing form submission data
        modelBuilder.Entity<ProblemFieldValue>(entity =>
        {
            // Primary key and table configuration
            entity.HasKey(e => e.Id);
            entity.ToTable("ProblemFieldValues");
            
            // Configure string properties
            entity.Property(e => e.Value)
                .HasMaxLength(4000); // Allow large text values
                
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Performance indexes for value queries
            entity.HasIndex(e => e.ProblemId)
                .HasDatabaseName("IX_ProblemFieldValues_ProblemId");
                
            entity.HasIndex(e => e.FormFieldId)
                .HasDatabaseName("IX_ProblemFieldValues_FormFieldId");
                
            entity.HasIndex(e => new { e.ProblemId, e.FormFieldId })
                .IsUnique()
                .HasDatabaseName("IX_ProblemFieldValues_Problem_FormField"); // Prevent duplicate values
            
            // Configure relationships
            entity.HasOne(e => e.Problem)
                .WithMany(p => p.FieldValues)
                .HasForeignKey(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade); // Remove values when problem is deleted
                
            entity.HasOne(e => e.FormField)
                .WithMany() // FormField doesn't have Values collection
                .HasForeignKey(e => e.FormFieldId)
                .OnDelete(DeleteBehavior.Restrict); // Keep values even if field definition is changed
        });
    }
    #endregion
}
