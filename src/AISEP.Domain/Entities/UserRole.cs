namespace AISEP.Domain.Entities;

public class UserRole
{
    public int UserRoleID { get; set; }
    public int UserID { get; set; }
    public int RoleID { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public User? AssignedByUser { get; set; }
}
