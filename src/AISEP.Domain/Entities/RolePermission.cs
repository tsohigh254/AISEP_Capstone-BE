namespace AISEP.Domain.Entities;

public class RolePermission
{
    public int RolePermissionID { get; set; }
    public int RoleID { get; set; }
    public int PermissionID { get; set; }

    // Navigation properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
