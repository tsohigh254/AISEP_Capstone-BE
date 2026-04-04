using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                from = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>",
                to = new[] { toEmail },
                subject,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _emailSettings.ResendApiKey);

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Resend API error: {StatusCode} {Body}", response.StatusCode, responseBody);
                throw new Exception($"Resend API error: {response.StatusCode} - {responseBody}");
            }

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
