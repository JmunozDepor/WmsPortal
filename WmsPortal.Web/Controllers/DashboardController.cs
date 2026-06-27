using Microsoft.AspNetCore.Mvc;
using WmsPortal.Core.Interfaces;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboard;
    private readonly IAuthService _auth;

    public DashboardController(IDashboardService dashboard, IAuthService auth)
    {
        _dashboard = dashboard;
        _auth = auth;
    }

    public async Task<IActionResult> Index(int dias = 1)
    {
        var redirect = RequireAuth();
        if (redirect != null) return redirect;

        var session = GetSession()!;
        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);

        var resumen = await _dashboard.GetResumenAsync(company, dias);

        return View(new DashboardViewModel
        {
            Resumen = resumen,
            Dias = dias,
            Company = company
        });
    }

    // AJAX: datos para Chart.js
    [HttpGet]
    public async Task<IActionResult> ChartData(int dias = 1)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { error = "no auth" });

        var session = GetSession()!;
        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);

        var resumen = await _dashboard.GetResumenAsync(company, dias);

        return Json(new
        {
            labels = resumen.PorTipo.Select(t => t.Tipo).ToArray(),
            ok = resumen.PorTipo.Select(t => t.Ok).ToArray(),
            error = resumen.PorTipo.Select(t => t.Error).ToArray(),
            pendiente = resumen.PorTipo.Select(t => t.Pendiente).ToArray(),
            totales = new
            {
                total = resumen.TotalTransacciones,
                ok = resumen.TotalOk,
                error = resumen.TotalError,
                pendiente = resumen.TotalPendiente
            }
        });
    }
}
