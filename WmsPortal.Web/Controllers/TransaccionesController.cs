using Microsoft.AspNetCore.Mvc;
using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class TransaccionesController : BaseController
{
    private readonly ITransaccionService _tx;
    private readonly IAuthService _auth;

    public TransaccionesController(ITransaccionService tx, IAuthService auth)
    {
        _tx = tx;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> Index(TipoTransaccion tipo = TipoTransaccion.EnvioProducto,
        string? status = null, string? numDoc = null, string? lpn = null, string? customerPo = null,
        string? desde = null, string? hasta = null,
        int page = 1, int pageSize = 10)
    {
        var redirect = RequireAuth();
        if (redirect != null) return redirect;

        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);
        var cols = await _tx.GetColumnsForTipoAsync(tipo);

        var filter = new TransaccionFilter
        {
            Tipo = tipo,
            Status = status,
            NumeroDocumento = numDoc,
            Lpn = lpn,
            CustomerPo = customerPo,
            // null = primer carga → defecto últimas 24h; "" = usuario limpió → sin filtro
            Desde = desde == null ? DateTime.Today.AddDays(-1)
                  : !string.IsNullOrEmpty(desde) ? DateTime.Parse(desde) : null,
            Hasta = hasta == null ? DateTime.Today
                  : !string.IsNullOrEmpty(hasta) ? DateTime.Parse(hasta) : null,
            Page = page,
            PageSize = pageSize
        };

        var result = await _tx.GetTransaccionesAsync(filter, company);

        return View(new TransaccionesViewModel
        {
            Filter = filter,
            Result = result,
            Columns = cols,
            Company = company,
            TipoLabel = TipoTransaccionInfo.Meta[tipo].Label
        });
    }

    [HttpPost]
    public async Task<IActionResult> Reset([FromBody] ResetRequest request)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });

        if (!CanReset)
            return Json(new { success = false, message = "Sin permisos para resetear transacciones." });

        var session = GetSession()!;
        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);

        string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _tx.ResetTransaccionesAsync(request, company, session.UserId, ip);

        return Json(new
        {
            success = result.Success,
            rowsAffected = result.RowsAffected,
            message = result.Success
                ? $"{result.RowsAffected} registro(s) reseteado(s) a PENDIENTE."
                : result.ErrorMessage
        });
    }
}
