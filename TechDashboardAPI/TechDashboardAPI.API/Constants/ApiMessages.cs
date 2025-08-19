namespace TechDashboardAPI.API.Constants;

public static class ApiMessages
{
    // Success Messages
    public const string LoginSuccessful = "Login successful";
    public const string UserRegistered = "User registered successfully";
    public const string UserUpdated = "User updated successfully";
    public const string UserActivated = "User activated successfully";
    public const string UserDeactivated = "User deactivated successfully";
    public const string RoleUpdated = "User role updated successfully";
    
    public const string DepartmentCreated = "Department created successfully";
    public const string DepartmentUpdated = "Department updated successfully";
    public const string DepartmentDeleted = "Department deleted successfully";
    
    public const string ProjectCreated = "Project created successfully";
    public const string ProjectUpdated = "Project updated successfully";
    public const string ProjectDeleted = "Project deleted successfully";
    
    public const string ProblemCreated = "Problem created successfully";
    public const string ProblemUpdated = "Problem updated successfully";
    public const string ProblemDeleted = "Problem deleted successfully";
    public const string ProblemLiked = "Problem liked successfully";
    public const string ProblemUnliked = "Problem unliked successfully";
    
    public const string SolutionSubmitted = "Solution submitted successfully";
    public const string SolutionUpdated = "Solution updated successfully";
    public const string SolutionDeleted = "Solution deleted successfully";
    public const string SolutionApproved = "Solution approved successfully";
    public const string SolutionRejected = "Solution rejected";
    
    public const string FormCreated = "Problem form created successfully";
    public const string FormUpdated = "Form updated successfully";
    public const string FormDeleted = "Form deleted successfully";
    
    // Error Messages
    public const string InvalidCredentials = "Invalid email or password";
    public const string UserNotFound = "User not found";
    public const string UserInactive = "User account is inactive";
    public const string DepartmentNotFound = "Department not found";
    public const string ProjectNotFound = "Project not found";
    public const string ProblemNotFound = "Problem not found";
    public const string SolutionNotFound = "Solution not found";
    public const string FormNotFound = "Form not found";
    public const string FileNotFound = "File not found";
    public const string UnauthorizedAccess = "You don't have permission to perform this action";
    public const string InvalidRole = "Invalid role specified";
    public const string EmailAlreadyExists = "Email already exists";
    public const string UsernameAlreadyExists = "Username already exists";
    public const string CannotDeactivateSelf = "You cannot deactivate your own account";
    public const string FileTooLarge = "File size exceeds maximum limit";
    public const string InvalidFileType = "Invalid file type";
    
    // General Messages
    public const string InternalServerError = "An internal server error occurred";
    public const string ValidationError = "Validation failed";
    public const string NotFound = "Resource not found";
    public const string BadRequest = "Bad request";
}
