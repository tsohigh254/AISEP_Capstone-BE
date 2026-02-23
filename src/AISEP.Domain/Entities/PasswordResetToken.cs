namespace AISEP.Domain.Entities;

public class PasswordResetToken
{
    public int PasswordResetTokenID { get; set; }
    public int UserID { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt != null;
    public bool IsValid => !IsUsed && !IsExpired;
    
    // Navigation
    public User User { get; set; } = null!;
}
