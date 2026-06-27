namespace WmsPortal.Core.Models;

public class PortalUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Viewer"; // Admin, Operador, Viewer
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public List<int> CompanyIds { get; set; } = new();
}

public class PortalCompany
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DbType { get; set; } = ""; // HANA, SQLSERVER
    public string ConnectionStr { get; set; } = "";
    public string SchemaName { get; set; } = "CLPRD_WMS";
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool ShowInLogin { get; set; } = true;
}

public class UserCompanyPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public bool CanReset { get; set; }
    public string CompanyName { get; set; } = "";
    public string CompanyCode { get; set; } = "";
    public string DbType { get; set; } = "";
}

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string Action { get; set; } = "RESET_PENDIENTE";
    public string TableName { get; set; } = "";
    public string RecordKey { get; set; } = "";
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "PENDIENTE";
    public DateTime ExecutedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Username { get; set; }
}
