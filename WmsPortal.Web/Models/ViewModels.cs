using WmsPortal.Core.Models;

namespace WmsPortal.Web.Models;

public class LoginViewModel
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int CompanyId { get; set; }
    public List<PortalCompany> Companies { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class DashboardViewModel
{
    public DashboardResumen Resumen { get; set; } = new();
    public int Dias { get; set; } = 1;
    public PortalCompany Company { get; set; } = new();
}

public class TransaccionesViewModel
{
    public TransaccionFilter Filter { get; set; } = new();
    public PagedResult<TransaccionRow> Result { get; set; } = new();
    public List<ColumnDef> Columns { get; set; } = new();
    public PortalCompany Company { get; set; } = new();
    public string TipoLabel { get; set; } = "";
}

public class AdminViewModel
{
    public List<PortalUser> Users { get; set; } = new();
    public List<PortalCompany> Companies { get; set; } = new();
    public PortalCompany Company { get; set; } = new();
}

public class CompanyFormRequest
{
    public int    Id           { get; set; }
    public string Code         { get; set; } = "";
    public string Name         { get; set; } = "";
    public string DbType       { get; set; } = "HANA";
    public string ConnStr      { get; set; } = "";
    public string SchemaName   { get; set; } = "CLPRD_WMS";
    public int    SortOrder    { get; set; } = 99;
    public bool   ShowInLogin  { get; set; } = true;
}

public class UserFormRequest
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public List<int> CompanyIds { get; set; } = new();
}

// ─── Datos de sesión ────────────────────────────────────────────────────────
public class SessionCompany
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string DbType { get; set; } = "";
    public string? LogoPath { get; set; }
}

public class SessionData
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "";
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string CompanyDbType { get; set; } = "";
    public string CompanyCode { get; set; } = "";
    public string? CompanyLogoPath { get; set; }
    public bool CanReset { get; set; }
    public List<SessionCompany> AvailableCompanies { get; set; } = new();
}

public class SwitchCompanyRequest
{
    public int CompanyId { get; set; }
}
