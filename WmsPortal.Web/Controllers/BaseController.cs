using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WmsPortal.Core.Models;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class BaseController : Controller
{
    protected SessionData? GetSession()
    {
        var json = HttpContext.Session.GetString("SessionData");
        return json == null ? null : JsonSerializer.Deserialize<SessionData>(json);
    }

    protected void SetSession(SessionData data)
    {
        HttpContext.Session.SetString("SessionData", JsonSerializer.Serialize(data));
    }

    protected void ClearSession() => HttpContext.Session.Clear();

    protected IActionResult RequireAuth()
    {
        var session = GetSession();
        if (session == null) return RedirectToAction("Login", "Auth");
        return null!;
    }

    protected PortalCompany GetCompanyFromSession(List<PortalCompany> companies)
    {
        var session = GetSession()!;
        return companies.FirstOrDefault(c => c.Id == session.CompanyId) ?? companies.First();
    }

    protected bool CanReset => GetSession()?.CanReset == true || GetSession()?.Role == "Admin";
}
