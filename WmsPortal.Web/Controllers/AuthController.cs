using Microsoft.AspNetCore.Mvc;
using WmsPortal.Core.Interfaces;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) { _auth = auth; }

    public IActionResult Index() => RedirectToAction("Login");

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (GetSession() != null) return RedirectToAction("Index", "Dashboard");

        var companies = await _auth.GetLoginCompaniesAsync();
        return View(new LoginViewModel { Companies = companies });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var companies = await _auth.GetLoginCompaniesAsync();
        model.Companies = companies;

        if (model.CompanyId == 0 && companies.Any())
        {
            model.CompanyId = companies.First().Id;
        }

        var company = companies.FirstOrDefault(c => c.Id == model.CompanyId);
        if (company == null)
        {
            model.ErrorMessage = "Selecciona una empresa válida.";
            return View(model);
        }

        var user = await _auth.ValidateLoginAsync(model.Username, model.Password, model.CompanyId);
        if (user == null)
        {
            model.ErrorMessage = "Usuario o contraseña incorrectos, o sin acceso a esta empresa.";
            return View(model);
        }

        await _auth.UpdateLastLoginAsync(user.Id);

        // Para el switcher en-app: todas las empresas activas a las que el usuario tiene acceso
        // (sin filtrar por ShowInLogin — esa restricción es solo para la pantalla de login)
        var allCompanies = await _auth.GetAllCompaniesAsync();
        var accessible = user.Role == "Admin"
            ? allCompanies
            : allCompanies.Where(c => user.CompanyIds.Contains(c.Id)).ToList();

        SetSession(new SessionData
        {
            UserId          = user.Id,
            Username        = user.Username,
            Role            = user.Role,
            CompanyId       = company.Id,
            CompanyName     = company.Name,
            CompanyDbType   = company.DbType,
            CompanyCode     = company.Code,
            CompanyLogoPath = company.LogoPath,
            CanReset        = user.Role is "Admin" or "Operador",
            AvailableCompanies = accessible.Select(c => new SessionCompany
            {
                Id = c.Id, Name = c.Name, Code = c.Code,
                DbType = c.DbType, LogoPath = c.LogoPath
            }).ToList()
        });

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult SwitchCompany([FromBody] SwitchCompanyRequest req)
    {
        var sd = GetSession();
        if (sd == null) return Json(new { success = false, message = "Sin sesión" });

        var target = sd.AvailableCompanies.FirstOrDefault(c => c.Id == req.CompanyId);
        if (target == null) return Json(new { success = false, message = "Sin acceso a esa empresa." });

        sd.CompanyId       = target.Id;
        sd.CompanyName     = target.Name;
        sd.CompanyCode     = target.Code;
        sd.CompanyDbType   = target.DbType;
        sd.CompanyLogoPath = target.LogoPath;
        SetSession(sd);

        return Json(new { success = true });
    }

    public IActionResult Logout()
    {
        ClearSession();
        return RedirectToAction("Login");
    }
}
