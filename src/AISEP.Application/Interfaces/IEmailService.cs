namespace AISEP.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl, CancellationToken cancellationToken = default);
    Task SendVerificationEmailAsync(string toEmail, string verificationToken, string verificationUrl, CancellationToken cancellationToken = default);
}
