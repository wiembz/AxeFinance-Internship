namespace TechDashboardAPI.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendWelcomeEmailAsync(string to, string username, string temporaryPassword);
    Task<bool> SendInvitationEmailAsync(string to, string inviterName, string invitationLink);
    Task<bool> SendPasswordResetEmailAsync(string to, string resetLink);
    Task<bool> SendEmailVerificationAsync(string to, string verificationLink);
}
