using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AISEP.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            var secureOption = _emailSettings.EnableSsl 
                ? SecureSocketOptions.StartTls 
                : SecureSocketOptions.None;

            await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, secureOption, cancellationToken);

            if (!string.IsNullOrEmpty(_emailSettings.SmtpUser))
            {
                await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl, CancellationToken cancellationToken = default)
    {
        var fullResetUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reset Your Password</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2563eb;'>Reset Your Password</h2>
        <p>You have requested to reset your password for your AISEP account.</p>
        <p>Click the button below to reset your password:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{fullResetUrl}' 
               style='background-color: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Reset Password
            </a>
        </div>
        <p>Or copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #666;'>{fullResetUrl}</p>
        <p><strong>This link will expire in 1 hour.</strong></p>
        <p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px;'>
            This is an automated message from AISEP. Please do not reply to this email.
        </p>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, "Reset Your Password - AISEP", htmlBody, cancellationToken);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationToken, string verificationUrl, CancellationToken cancellationToken = default)
    {
        var fullVerificationUrl = $"{verificationUrl}?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(toEmail)}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify Your Email</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2563eb;'>Welcome to AISEP!</h2>
        <p>Thank you for registering. Please verify your email address to complete your registration.</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{fullVerificationUrl}' 
               style='background-color: #10b981; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Verify Email
            </a>
        </div>
        <p>Or copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #666;'>{fullVerificationUrl}</p>
        <p><strong>This link will expire in 24 hours.</strong></p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='color: #666; font-size: 12px;'>
            This is an automated message from AISEP. Please do not reply to this email.
        </p>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, "Verify Your Email - AISEP", htmlBody, cancellationToken);
    }
}
