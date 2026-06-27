using BCrypt.Net;
using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Data.Repositories;

namespace WmsPortal.Web.Services;

public class AuthService : IAuthService
{
    private readonly UserRepository _userRepo;

    public AuthService(UserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<PortalUser?> ValidateLoginAsync(string username, string password, int companyId)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null || !user.IsActive) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var perms = await _userRepo.GetUserCompaniesAsync(user.Id);
        bool hasAccess = perms.Any(p => p.CompanyId == companyId);
        if (!hasAccess && user.Role != "Admin") return null;

        user.CompanyIds = perms.Select(p => p.CompanyId).ToList();
        return user;
    }

    public async Task<List<PortalCompany>> GetCompaniesForUserAsync(string username)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null) return new();

        var perms = await _userRepo.GetUserCompaniesAsync(user.Id);
        var all = await _userRepo.GetAllCompaniesAsync();

        if (user.Role == "Admin") return all;
        return all.Where(c => perms.Any(p => p.CompanyId == c.Id)).ToList();
    }

    public async Task<List<PortalCompany>> GetAllCompaniesAsync()
        => await _userRepo.GetAllCompaniesAsync();

    public async Task<List<PortalCompany>> GetLoginCompaniesAsync()
        => await _userRepo.GetLoginCompaniesAsync();

    public async Task<bool> CreateUserAsync(PortalUser user, string plainPassword, List<int> companyIds)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword, 11);
        int newId = await _userRepo.InsertUserAsync(user);
        if (newId <= 0) return false;
        foreach (var cid in companyIds)
            await _userRepo.InsertUserCompanyAsync(newId, cid);
        return true;
    }

    public async Task<bool> UpdateUserAsync(PortalUser user, List<int> companyIds)
    {
        await _userRepo.UpdateUserDataAsync(user);
        await _userRepo.DeleteUserCompaniesAsync(user.Id);
        foreach (var cid in companyIds)
            await _userRepo.InsertUserCompanyAsync(user.Id, cid);
        return true;
    }

    public async Task<bool> CreateCompanyAsync(PortalCompany company)
    {
        await _userRepo.InsertCompanyAsync(company);
        return true;
    }

    public async Task<bool> UpdateCompanyAsync(PortalCompany company)
    {
        await _userRepo.UpdateCompanyAsync(company);
        return true;
    }

    public async Task<List<PortalUser>> GetUsersAsync()
        => await _userRepo.GetAllUsersAsync();

    public async Task<bool> UpdateLastLoginAsync(int userId)
    {
        await _userRepo.UpdateLastLoginAsync(userId);
        return true;
    }
}
