namespace TechDashboardAPI.API.Constants;

public static class AppConstants
{
    // File Upload Configuration
    public const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    public static readonly string[] AllowedFileTypes = { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif" };
    public const string UploadsDirectory = "wwwroot/uploads";
    
    // Pagination
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    
    // JWT Configuration
    public const string JwtSettingsSection = "JwtSettings";
    public const string SecretKey = "SecretKey";
    public const string Issuer = "Issuer";
    public const string Audience = "Audience";
    public const string ExpirationHours = "ExpirationHours";
    
    // Database
    public const string DefaultConnection = "DefaultConnection";
    
    // Roles
    public const string AdminRole = "Admin";
    public const string ContributorRole = "Contributor";
    public const string ViewerRole = "Viewer";
    
    // Claims
    public const string DepartmentIdClaim = "DepartmentId";
    
    // Cache Keys
    public const string DepartmentsCacheKey = "departments_cache";
    public const string ProjectsCacheKey = "projects_cache";
    public const int DefaultCacheExpirationMinutes = 30;
}
