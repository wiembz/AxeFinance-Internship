using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using TechDashboardAPI.Application.Interfaces;

namespace TechDashboardAPI.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            var smtpUsername = emailSettings["SmtpUsername"];
            var smtpPassword = emailSettings["SmtpPassword"];
            var fromEmail = emailSettings["FromEmail"] ?? smtpUsername;
            var fromName = emailSettings["FromName"] ?? "Tech Dashboard";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("Email settings are not configured properly. Email not sent.");
                return false;
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            // Guard against null/empty addresses and names
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("FromEmail is not configured. Email not sent.");
                return false;
            }
            fromName ??= "Tech Dashboard";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName!),
                Subject = subject ?? string.Empty,
                Body = body ?? string.Empty,
                IsBodyHtml = isHtml
            };

            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Recipient address is empty. Email not sent.");
                return false;
            }
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string username, string temporaryPassword)
    {
        var subject = "Welcome to Tech Dashboard";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to Tech Dashboard, {username}!</h2>
                <p>Your account has been created successfully. Here are your login credentials:</p>
                <p><strong>Email:</strong> {to}</p>
                <p><strong>Temporary Password:</strong> {temporaryPassword}</p>
                <p><strong>Please change your password after your first login.</strong></p>
                <p>You can access the dashboard at: <a href=""{_configuration["ClientUrl"]}"">Tech Dashboard</a></p>
                <br>
                <p>Best regards,<br>Tech Dashboard Team</p>
            </body>
            </html>";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendInvitationEmailAsync(string to, string inviterName, string invitationLink)
    {
        var subject = "You're invited to join Tech Dashboard";
        var body = $@"
            <html>
            <body>
                <h2>You're invited to join Tech Dashboard!</h2>
                <p>{inviterName} has invited you to join the Tech Dashboard platform.</p>
                <p>Click the link below to accept the invitation and create your account:</p>
                <p><a href=""{invitationLink}"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Accept Invitation</a></p>
                <p>If you can't click the button, copy and paste this link into your browser:</p>
                <p>{invitationLink}</p>
                <p>This invitation will expire in 7 days.</p>
                <br>
                <p>Best regards,<br>Tech Dashboard Team</p>
            </body>
            </html>";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink)
    {
        var subject = "Reset your Tech Dashboard password";
        var body = $@"
            <html>
            <body>
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your Tech Dashboard password.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href=""{resetLink}"" style=""background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
                <p>If you can't click the button, copy and paste this link into your browser:</p>
                <p>{resetLink}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request this password reset, you can safely ignore this email.</p>
                <br>
                <p>Best regards,<br>Tech Dashboard Team</p>
            </body>
            </html>";

        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendEmailVerificationAsync(string to, string verificationLink)
    {
        var subject = "Verify your Tech Dashboard email address";
        var body = $@"
            <html>
            <body>
                <h2>Email Verification</h2>
                <p>Please verify your email address to complete your Tech Dashboard registration.</p>
                <p>Click the link below to verify your email:</p>
                <p><a href=""{verificationLink}"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Verify Email</a></p>
                <p>If you can't click the button, copy and paste this link into your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>Tech Dashboard Team</p>
            </body>
            </html>";

        return await SendEmailAsync(to, subject, body);
    }
}
