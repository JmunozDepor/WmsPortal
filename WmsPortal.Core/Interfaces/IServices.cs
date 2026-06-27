using WmsPortal.Core.Models;

namespace WmsPortal.Core.Interfaces;

public interface IAuthService
{
    Task<PortalUser?> ValidateLoginAsync(string username, string password, int companyId);
    Task<List<PortalCompany>> GetCompaniesForUserAsync(string username);
    Task<List<PortalCompany>> GetAllCompaniesAsync();
    Task<List<PortalCompany>> GetLoginCompaniesAsync();
    Task<bool> CreateUserAsync(PortalUser user, string plainPassword, List<int> companyIds);
    Task<bool> UpdateUserAsync(PortalUser user, List<int> companyIds);
    Task<bool> CreateCompanyAsync(PortalCompany company);
    Task<bool> UpdateCompanyAsync(PortalCompany company);
    Task<List<PortalUser>> GetUsersAsync();
    Task<bool> UpdateLastLoginAsync(int userId);
}

public interface ITransaccionService
{
    Task<PagedResult<TransaccionRow>> GetTransaccionesAsync(TransaccionFilter filter, PortalCompany company);
    Task<ResetResult> ResetTransaccionesAsync(ResetRequest request, PortalCompany company, int userId, string ipAddress);
    Task<List<ColumnDef>> GetColumnsForTipoAsync(TipoTransaccion tipo);
}

public interface IDashboardService
{
    Task<DashboardResumen> GetResumenAsync(PortalCompany company, int dias);
}

public interface IAuditService
{
    Task LogResetAsync(AuditLog entry, PortalCompany company);
    Task<List<AuditLog>> GetAuditLogAsync(PortalCompany company, int page, int pageSize);
}
