using Microsoft.AspNetCore.Mvc;
using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class WmsStageController : BaseController
{
    private readonly IWmsStageService _svc;
    private readonly IAuthService _auth;

    public WmsStageController(IWmsStageService svc, IAuthService auth)
    {
        _svc = svc;
        _auth = auth;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? estado = null, string? nombreArchivo = null,
        string? desde = null, string? hasta = null,
        int page = 1, int pageSize = 10)
    {
        var redirect = RequireAuth();
        if (redirect != null) return redirect;

        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);

        var filter = new WmsStageFilter
        {
            Estado        = estado,
            NombreArchivo = nombreArchivo,
            Desde = desde == null ? DateTime.Today.AddDays(-1)
                  : !string.IsNullOrEmpty(desde) ? DateTime.Parse(desde) : null,
            Hasta = hasta == null ? DateTime.Today
                  : !string.IsNullOrEmpty(hasta) ? DateTime.Parse(hasta) : null,
            Page     = page,
            PageSize = pageSize
        };

        var result = await _svc.GetArchivosPaginadoAsync(filter, company);

        return View(new WmsStageViewModel
        {
            Filter  = filter,
            Result  = result,
            Company = company
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { error = "Sin sesión" });

        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);
        var xml = await _svc.GetContenidoXmlAsync(id, company);
        return Json(new { xml });
    }
}
