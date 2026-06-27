using Microsoft.AspNetCore.Mvc;
using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Data.Providers;
using WmsPortal.Web.Models;

namespace WmsPortal.Web.Controllers;

public class AdminController : BaseController
{
    private readonly IAuthService _auth;
    private readonly IDbConnectionFactory _dbFactory;

    public AdminController(IAuthService auth, IDbConnectionFactory dbFactory)
    {
        _auth = auth;
        _dbFactory = dbFactory;
    }

    public async Task<IActionResult> Index()
    {
        var redirect = RequireAuth();
        if (redirect != null) return redirect;

        var session = GetSession()!;
        if (session.Role != "Admin") return RedirectToAction("Index", "Dashboard");

        var companies = await _auth.GetAllCompaniesAsync();
        var company = GetCompanyFromSession(companies);

        return View(new AdminViewModel
        {
            Users = await _auth.GetUsersAsync(),
            Companies = companies,
            Company = company
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CompanyFormRequest req)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });
        if (GetSession()?.Role != "Admin") return Json(new { success = false, message = "Sin permisos" });

        if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.ConnStr))
            return Json(new { success = false, message = "Código, nombre y cadena de conexión son requeridos." });

        var company = new PortalCompany
        {
            Code          = req.Code.Trim().ToUpper(),
            Name          = req.Name.Trim(),
            DbType        = req.DbType,
            ConnectionStr = req.ConnStr.Trim(),
            SchemaName    = req.SchemaName.Trim(),
            SortOrder     = req.SortOrder,
            IsActive      = true,
            ShowInLogin   = req.ShowInLogin
        };

        try
        {
            var ok = await _auth.CreateCompanyAsync(company);
            return Json(new { success = ok, message = ok ? "Empresa creada correctamente." : "Error al crear empresa." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCompany([FromBody] CompanyFormRequest req)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });
        if (GetSession()?.Role != "Admin") return Json(new { success = false, message = "Sin permisos" });

        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.ConnStr))
            return Json(new { success = false, message = "Nombre y cadena de conexión son requeridos." });

        var company = new PortalCompany
        {
            Id            = req.Id,
            Name          = req.Name.Trim(),
            DbType        = req.DbType,
            ConnectionStr = req.ConnStr.Trim(),
            SchemaName    = req.SchemaName.Trim(),
            ShowInLogin   = req.ShowInLogin
        };

        try
        {
            var ok = await _auth.UpdateCompanyAsync(company);
            return Json(new { success = ok, message = ok ? "Empresa actualizada correctamente." : "Error al actualizar." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserFormRequest req)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });
        if (GetSession()?.Role != "Admin") return Json(new { success = false, message = "Sin permisos" });

        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Json(new { success = false, message = "Usuario, email y contraseña son requeridos." });

        var user = new PortalUser
        {
            Username = req.Username.Trim(),
            Email    = req.Email.Trim(),
            Role     = req.Role,
            IsActive = req.IsActive
        };

        try
        {
            var ok = await _auth.CreateUserAsync(user, req.Password, req.CompanyIds);
            return Json(new { success = ok, message = ok ? "Usuario creado correctamente." : "Error al crear usuario." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult TestConnection([FromBody] CompanyFormRequest req)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });
        if (GetSession()?.Role != "Admin") return Json(new { success = false, message = "Sin permisos" });

        if (string.IsNullOrWhiteSpace(req.ConnStr) || string.IsNullOrWhiteSpace(req.DbType))
            return Json(new { success = false, message = "Tipo de BD y cadena de conexión son requeridos." });

        try
        {
            using var conn = _dbFactory.CreateConnection(req.DbType, req.ConnStr);
            conn.Open();
            return Json(new { success = true, message = $"Conexión exitosa ({req.DbType})." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromBody] UserFormRequest req)
    {
        var redirect = RequireAuth();
        if (redirect != null) return Json(new { success = false, message = "Sin sesión" });
        if (GetSession()?.Role != "Admin") return Json(new { success = false, message = "Sin permisos" });

        if (string.IsNullOrWhiteSpace(req.Email))
            return Json(new { success = false, message = "El email es requerido." });

        var user = new PortalUser
        {
            Id           = req.Id,
            Email        = req.Email.Trim(),
            Role         = req.Role,
            IsActive     = req.IsActive,
            PasswordHash = string.IsNullOrEmpty(req.Password)
                               ? ""
                               : BCrypt.Net.BCrypt.HashPassword(req.Password, 11)
        };

        try
        {
            var ok = await _auth.UpdateUserAsync(user, req.CompanyIds);
            return Json(new { success = ok, message = ok ? "Usuario actualizado correctamente." : "Error al actualizar." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}
