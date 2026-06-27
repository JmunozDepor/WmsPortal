using Dapper;
using WmsPortal.Core.Models;
using WmsPortal.Data.Providers;
using WmsPortal.Data.QueryBuilders;

namespace WmsPortal.Data.Repositories;

/// <summary>
/// Opera siempre contra la BD maestra (HANA - Depor).
/// Los datos de usuarios/empresas se replican manualmente a SQL Server.
/// </summary>
public class UserRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly string _masterDbType;
    private readonly string _masterConnStr;
    private readonly string _schema;

    public UserRepository(IDbConnectionFactory factory,
        string masterDbType, string masterConnStr, string schema = "CLPRD_WMS")
    {
        _factory = factory;
        _masterDbType = masterDbType;
        _masterConnStr = masterConnStr;
        _schema = schema;
    }

    public async Task<PortalUser?> GetByUsernameAsync(string username)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();

        string sql = QueryFactory.GetUserByUsername(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            // HANA usa parámetros posicionales con ?
            sql = sql.Replace("@username", $"'{username}'");
            var row = await conn.QueryFirstOrDefaultAsync(sql);
            if (row == null) return null;
            return MapUser(row);
        }
        else
        {
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { username });
            if (row == null) return null;
            return MapUser(row);
        }
    }

    public async Task<List<UserCompanyPermission>> GetUserCompaniesAsync(int userId)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();

        string sql = QueryFactory.GetUserCompanies(_masterDbType, _schema);

        IEnumerable<dynamic> rows;
        if (_masterDbType == "HANA")
        {
            sql = sql.Replace("@userId", userId.ToString());
            rows = await conn.QueryAsync(sql);
        }
        else
        {
            rows = await conn.QueryAsync(sql, new { userId });
        }

        return rows.Select(r => new UserCompanyPermission
        {
            CompanyId = (int)GetVal(r, "COMPANY_ID"),
            CanReset = GetIntVal(r, "CAN_RESET") == 1,
            CompanyName = GetStrVal(r, "CompanyName") ?? "",
            CompanyCode = GetStrVal(r, "CompanyCode") ?? "",
            DbType = GetStrVal(r, "DbType") ?? ""
        }).ToList();
    }

    public async Task<List<PortalCompany>> GetAllCompaniesAsync()
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.GetAllCompanies(_masterDbType, _schema);
        var rows = await conn.QueryAsync(sql);
        return rows.Select(MapCompany).ToList();
    }

    public async Task<List<PortalCompany>> GetLoginCompaniesAsync()
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        try
        {
            string sql = QueryFactory.GetLoginCompanies(_masterDbType, _schema);
            var rows = await conn.QueryAsync(sql);
            return rows.Select(MapCompany).ToList();
        }
        catch
        {
            // Columna SHOW_IN_LOGIN aún no existe — devolver todas las empresas activas
            string sql = QueryFactory.GetAllCompanies(_masterDbType, _schema);
            var rows = await conn.QueryAsync(sql);
            return rows.Select(MapCompany).ToList();
        }
    }

    private static PortalCompany MapCompany(dynamic r) => new()
    {
        Id            = (int)GetVal(r, "ID"),
        Code          = GetStrVal(r, "CODE") ?? "",
        Name          = GetStrVal(r, "NAME") ?? "",
        DbType        = GetStrVal(r, "DB_TYPE") ?? "",
        ConnectionStr = GetStrVal(r, "CONNECTION_STR") ?? "",
        SchemaName    = GetStrVal(r, "SCHEMA_NAME") ?? "CLPRD_WMS",
        LogoPath      = GetStrVal(r, "LOGO_PATH"),
        IsActive      = GetIntVal(r, "IS_ACTIVE") == 1,
        SortOrder     = GetIntVal(r, "SORT_ORDER"),
        ShowInLogin   = GetIntVal(r, "SHOW_IN_LOGIN", defaultVal: 1) == 1
    };

    public async Task<List<PortalUser>> GetAllUsersAsync()
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string tbl = QueryFactory.TableRef(_masterDbType, _schema, "PORTAL_USERS");
        string sql = $"SELECT * FROM {tbl} ORDER BY {QueryFactory.Col(_masterDbType,"ID")}";
        var rows = await conn.QueryAsync(sql);
        var users = rows.Select(MapUser).ToList();

        foreach (var u in users)
        {
            var perms = await GetUserCompaniesAsync(u.Id);
            u.CompanyIds = perms.Select(p => p.CompanyId).ToList();
        }

        return users;
    }

    public async Task<int> InsertUserAsync(PortalUser user)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.InsertUser(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            sql = sql
                .Replace("@username", $"'{Esc(user.Username)}'")
                .Replace("@email",    $"'{Esc(user.Email)}'")
                .Replace("@pwdhash",  $"'{Esc(user.PasswordHash)}'")
                .Replace("@role",     $"'{Esc(user.Role)}'")
                .Replace("@isActive", user.IsActive ? "1" : "0");
            await conn.ExecuteAsync(sql);

            string tbl    = QueryFactory.TableRef(_masterDbType, _schema, "PORTAL_USERS");
            string idCol   = QueryFactory.Col(_masterDbType, "ID");
            string unameCol = QueryFactory.Col(_masterDbType, "USERNAME");
            var result = await conn.QueryFirstOrDefaultAsync(
                $"SELECT {idCol} FROM {tbl} WHERE {unameCol} = '{Esc(user.Username)}'");
            if (result == null) return 0;
            return Convert.ToInt32(((IDictionary<string, object>)result).Values.First());
        }
        else
        {
            return await conn.ExecuteScalarAsync<int>(sql + "; SELECT SCOPE_IDENTITY()", new
            {
                username = user.Username,
                email    = user.Email,
                pwdhash  = user.PasswordHash,
                role     = user.Role,
                isActive = user.IsActive ? 1 : 0
            });
        }
    }

    public async Task UpdateUserDataAsync(PortalUser user)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.UpdateUser(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            sql = sql
                .Replace("@email",    $"'{Esc(user.Email)}'")
                .Replace("@role",     $"'{Esc(user.Role)}'")
                .Replace("@isActive", user.IsActive ? "1" : "0")
                .Replace("@userId",   user.Id.ToString());
            await conn.ExecuteAsync(sql);

            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                string pwdSql = QueryFactory.UpdateUserPassword(_masterDbType, _schema)
                    .Replace("@pwdhash", $"'{Esc(user.PasswordHash)}'")
                    .Replace("@userId",  user.Id.ToString());
                await conn.ExecuteAsync(pwdSql);
            }
        }
        else
        {
            await conn.ExecuteAsync(sql, new { email = user.Email, role = user.Role, isActive = user.IsActive ? 1 : 0, userId = user.Id });

            if (!string.IsNullOrEmpty(user.PasswordHash))
                await conn.ExecuteAsync(QueryFactory.UpdateUserPassword(_masterDbType, _schema),
                    new { pwdhash = user.PasswordHash, userId = user.Id });
        }
    }

    public async Task InsertUserCompanyAsync(int userId, int companyId)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.InsertUserCompany(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            sql = sql.Replace("@userId", userId.ToString()).Replace("@companyId", companyId.ToString());
            await conn.ExecuteAsync(sql);
        }
        else
        {
            await conn.ExecuteAsync(sql, new { userId, companyId });
        }
    }

    public async Task DeleteUserCompaniesAsync(int userId)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.DeleteUserCompanies(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            sql = sql.Replace("@userId", userId.ToString());
            await conn.ExecuteAsync(sql);
        }
        else
        {
            await conn.ExecuteAsync(sql, new { userId });
        }
    }

    public async Task InsertCompanyAsync(PortalCompany company)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.InsertCompany(_masterDbType, _schema);
        int showInLoginVal = company.ShowInLogin ? 1 : 0;

        if (_masterDbType == "HANA")
        {
            sql = sql
                .Replace("@code",         $"'{Esc(company.Code)}'")
                .Replace("@cname",        $"'{Esc(company.Name)}'")
                .Replace("@cdbType",      $"'{Esc(company.DbType)}'")
                .Replace("@connStr",      $"'{Esc(company.ConnectionStr)}'")
                .Replace("@schemaName",   $"'{Esc(company.SchemaName)}'")
                .Replace("@sortOrder",    company.SortOrder.ToString())
                .Replace("@showInLogin",  showInLoginVal.ToString());
            await conn.ExecuteAsync(sql);
        }
        else
        {
            await conn.ExecuteAsync(sql, new
            {
                code        = company.Code,
                cname       = company.Name,
                cdbType     = company.DbType,
                connStr     = company.ConnectionStr,
                schemaName  = company.SchemaName,
                sortOrder   = company.SortOrder,
                showInLogin = showInLoginVal
            });
        }
    }

    public async Task UpdateCompanyAsync(PortalCompany company)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.UpdateCompany(_masterDbType, _schema);
        int showInLoginVal = company.ShowInLogin ? 1 : 0;

        if (_masterDbType == "HANA")
        {
            sql = sql
                .Replace("@cname",        $"'{Esc(company.Name)}'")
                .Replace("@cdbType",      $"'{Esc(company.DbType)}'")
                .Replace("@connStr",      $"'{Esc(company.ConnectionStr)}'")
                .Replace("@schemaName",   $"'{Esc(company.SchemaName)}'")
                .Replace("@showInLogin",  showInLoginVal.ToString())
                .Replace("@compId",       company.Id.ToString());
            await conn.ExecuteAsync(sql);
        }
        else
        {
            await conn.ExecuteAsync(sql, new
            {
                cname       = company.Name,
                cdbType     = company.DbType,
                connStr     = company.ConnectionStr,
                schemaName  = company.SchemaName,
                showInLogin = showInLoginVal,
                compId      = company.Id
            });
        }
    }

    public async Task InsertAuditAsync(AuditLog entry)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string sql = QueryFactory.InsertAuditLog(_masterDbType, _schema);

        if (_masterDbType == "HANA")
        {
            // HANA: reemplazar parámetros nombrados por valores literales
            sql = sql
                .Replace(":userId", entry.UserId.ToString())
                .Replace(":companyId", entry.CompanyId.ToString())
                .Replace(":action", $"'{entry.Action}'")
                .Replace(":tableName", $"'{entry.TableName}'")
                .Replace(":recordKey", $"'{entry.RecordKey.Replace("'","''")}'")
                .Replace(":oldStatus", entry.OldStatus != null ? $"'{entry.OldStatus}'" : "NULL")
                .Replace(":newStatus", $"'{entry.NewStatus}'")
                .Replace(":ipAddress", entry.IpAddress != null ? $"'{entry.IpAddress}'" : "NULL");
            await conn.ExecuteAsync(sql);
        }
        else
        {
            await conn.ExecuteAsync(sql, new
            {
                userId = entry.UserId,
                companyId = entry.CompanyId,
                action = entry.Action,
                tableName = entry.TableName,
                recordKey = entry.RecordKey,
                oldStatus = entry.OldStatus,
                newStatus = entry.NewStatus,
                ipAddress = entry.IpAddress
            });
        }
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var conn = _factory.CreateConnection(_masterDbType, _masterConnStr);
        conn.Open();
        string tbl = QueryFactory.TableRef(_masterDbType, _schema, "PORTAL_USERS");
        string col_id = QueryFactory.Col(_masterDbType, "ID");
        string col_ll = QueryFactory.Col(_masterDbType, "LAST_LOGIN");
        string now = _masterDbType == "HANA" ? "CURRENT_TIMESTAMP" : "GETDATE()";
        string sql = $"UPDATE {tbl} SET {col_ll} = {now} WHERE {col_id} = {userId}";
        await conn.ExecuteAsync(sql);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string Esc(string s) => s.Replace("'", "''");

    private static PortalUser MapUser(dynamic r) => new()
    {
        Id = (int)GetVal(r, "ID"),
        Username = GetStrVal(r, "USERNAME") ?? "",
        Email = GetStrVal(r, "EMAIL") ?? "",
        PasswordHash = GetStrVal(r, "PASSWORD_HASH") ?? "",
        Role = GetStrVal(r, "ROLE") ?? "Viewer",
        IsActive = GetIntVal(r, "IS_ACTIVE") == 1
    };

    private static object GetVal(dynamic row, string key)
    {
        var dict = (IDictionary<string, object>)row;
        return dict.TryGetValue(key, out var v) ? v : 0;
    }

    private static string? GetStrVal(dynamic row, string key)
    {
        var dict = (IDictionary<string, object>)row;
        return dict.TryGetValue(key, out var v) ? v?.ToString() : null;
    }

    private static int GetIntVal(dynamic row, string key, int defaultVal = 0)
    {
        var dict = (IDictionary<string, object>)row;
        if (!dict.TryGetValue(key, out var v)) return defaultVal;
        try { return Convert.ToInt32(v); }
        catch { return defaultVal; }
    }
}
