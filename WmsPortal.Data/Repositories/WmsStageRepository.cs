using Dapper;
using WmsPortal.Core.Models;
using WmsPortal.Data.Providers;
using WmsPortal.Data.QueryBuilders;

namespace WmsPortal.Data.Repositories;

public class WmsStageRepository
{
    private readonly IDbConnectionFactory _factory;

    public WmsStageRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<(List<dynamic> Rows, int Total)> GetPagedAsync(
        PortalCompany company, WmsStageFilter filter)
    {
        using var conn = _factory.CreateConnection(company.DbType, company.ConnectionStr);
        conn.Open();

        var (dataSql, countSql) = QueryFactory.BuildWmsStageQuery(
            company.DbType, company.SchemaName,
            filter.Estado, filter.NombreArchivo,
            filter.Desde, filter.Hasta,
            filter.Page, filter.PageSize);

        var rows = (await conn.QueryAsync(dataSql)).AsList();
        int total = await conn.ExecuteScalarAsync<int>(countSql);
        return (rows, total);
    }

    public async Task<string?> GetContenidoXmlAsync(PortalCompany company, long id)
    {
        using var conn = _factory.CreateConnection(company.DbType, company.ConnectionStr);
        conn.Open();
        string sql = QueryFactory.GetWmsStageXmlById(company.DbType, company.SchemaName, id);
        return await conn.ExecuteScalarAsync<string>(sql);
    }
}
