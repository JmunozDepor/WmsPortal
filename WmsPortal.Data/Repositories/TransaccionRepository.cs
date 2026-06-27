using System.Data;
using Dapper;
using WmsPortal.Core.Models;
using WmsPortal.Data.Providers;
using WmsPortal.Data.QueryBuilders;

namespace WmsPortal.Data.Repositories;

public class TransaccionRepository
{
    private readonly IDbConnectionFactory _factory;

    public TransaccionRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<(List<dynamic> Rows, int Total)> GetPagedAsync(
        PortalCompany company, TransaccionFilter filter)
    {
        using var conn = _factory.CreateConnection(company.DbType, company.ConnectionStr);
        conn.Open();

        var (dataSql, countSql) = QueryFactory.BuildTransaccionQuery(
            company.DbType, company.SchemaName, filter.Tipo,
            filter.Status, filter.NumeroDocumento, filter.Lpn, filter.CustomerPo,
            filter.Desde, filter.Hasta,
            filter.Page, filter.PageSize);

        var rows = (await conn.QueryAsync(dataSql)).AsList();
        int total = await conn.ExecuteScalarAsync<int>(countSql);

        return (rows, total);
    }

    public async Task<int> ResetAsync(
        PortalCompany company, TipoTransaccion tipo, List<string> claves)
    {
        if (claves.Count == 0) return 0;

        using var conn = _factory.CreateConnection(company.DbType, company.ConnectionStr);
        conn.Open();

        string tableName = TipoTransaccionInfo.Meta[tipo].TableName;
        string pkField = QueryFactory.Col(company.DbType, TipoTransaccionInfo.Meta[tipo].PkField);
        string tbl = QueryFactory.TableRef(company.DbType, company.SchemaName, tableName);
        string status = QueryFactory.Col(company.DbType, "Status");
        string retry = QueryFactory.Col(company.DbType, "Retry_Count");
        string errLog = QueryFactory.Col(company.DbType, "Sync_Error_Log");

        // Construir parámetros según motor
        int total = 0;
        if (company.DbType == "HANA")
        {
            // HANA: ejecutar uno por uno con parámetro posicional
            foreach (var clave in claves)
            {
                string sql = $@"UPDATE {tbl}
SET {status} = 'PENDIENTE', {retry} = 0, {errLog} = NULL
WHERE {pkField} = '{clave}'";
                total += await conn.ExecuteAsync(sql);
            }
        }
        else
        {
            // SQL Server: usar parámetros con Dapper
            string paramList = string.Join(",", claves.Select((_, i) => $"@p{i}"));
            string sql = $@"UPDATE {tbl}
SET {status} = 'PENDIENTE', {retry} = 0, {errLog} = NULL
WHERE {pkField} IN ({paramList})";

            var dp = new DynamicParameters();
            for (int i = 0; i < claves.Count; i++)
                dp.Add($"p{i}", claves[i]);

            total = await conn.ExecuteAsync(sql, dp);
        }

        return total;
    }

    public async Task<List<dynamic>> GetDashboardCountsAsync(
        PortalCompany company, string tableName, string dateField,
        string excludeStatus, int dias, string[]? groupByCols = null)
    {
        using var conn = _factory.CreateConnection(company.DbType, company.ConnectionStr);
        conn.Open();

        string sql = QueryFactory.BuildDashboardCountQuery(
            company.DbType, company.SchemaName, tableName, dateField, excludeStatus, dias, groupByCols);

        var result = await conn.QueryAsync(sql);
        return result.AsList();
    }
}
