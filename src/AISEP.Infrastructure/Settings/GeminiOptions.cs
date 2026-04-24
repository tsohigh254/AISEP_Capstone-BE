namespace AISEP.Infrastructure.Settings;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Location { get; set; } = "us-central1";
    public string Model { get; set; } = "gemini-1.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
