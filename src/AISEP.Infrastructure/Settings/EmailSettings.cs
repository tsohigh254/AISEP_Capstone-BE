namespace AISEP.Infrastructure.Settings;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "AISEP";
    public bool EnableSsl { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:5294";
    public string ResendApiKey { get; set; } = string.Empty;
}
