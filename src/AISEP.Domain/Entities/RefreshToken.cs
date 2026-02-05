namespace AISEP.Domain.Entities;

public class RefreshToken
{
    public int RefreshTokenID { get; set; }
    public int UserID { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation
    public User User { get; set; } = null!;
}
