using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Data.Repositories;

namespace WmsPortal.Web.Services;

public class WmsStageService : IWmsStageService
{
    private readonly WmsStageRepository _repo;

    public WmsStageService(WmsStageRepository repo) => _repo = repo;

    public async Task<PagedResult<WmsStageRow>> GetArchivosPaginadoAsync(
        WmsStageFilter filter, PortalCompany company)
    {
        var (rows, total) = await _repo.GetPagedAsync(company, filter);
        return new PagedResult<WmsStageRow>
        {
            Items = rows.Select(r => (WmsStageRow)MapRow(r)).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<string?> GetContenidoXmlAsync(long id, PortalCompany company) =>
        await _repo.GetContenidoXmlAsync(company, id);

    private static WmsStageRow MapRow(dynamic r)
    {
        var d = (IDictionary<string, object>)r;
        string? Get(string key)
        {
            var found = d.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            return found != null ? d[found]?.ToString() : null;
        }
        return new WmsStageRow
        {
            Id         = long.TryParse(Get("ID"), out var id) ? id : 0,
            TipoDoc    = Get("TipoDoc"),
            NombreArchivo = Get("NombreArchivo"),
            Estado     = Get("Estado"),
            Intentos   = int.TryParse(Get("Intentos"), out var it) ? it : null,
            MensajeError = Get("MensajeError"),
            FechaInserto = Get("FechaInserto"),
            FechaProceso = Get("FechaProceso"),
        };
    }
}
