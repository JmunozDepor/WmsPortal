using System.Data;
using Sap.Data.Hana;
using Microsoft.Data.SqlClient;

namespace WmsPortal.Data.Providers;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection(string dbType, string connectionString);
}

public class DbConnectionFactory : IDbConnectionFactory
{
    // HANA topology redirect devuelve el FQDN que no resuelve en red local;
    // lo reemplazamos por la IP para que ambas conexiones usen el mismo host.
    private static readonly Dictionary<string, string> _hanaHostAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hw-depor-hdb.depor.cloud"] = "172.16.122.48"
    };

    public IDbConnection CreateConnection(string dbType, string connectionString)
    {
        return dbType.ToUpperInvariant() switch
        {
            "HANA" => new HanaConnection(NormalizeHanaConnStr(connectionString)),
            "SQLSERVER" => new SqlConnection(NormalizeSqlConnStr(connectionString)),
            _ => throw new NotSupportedException($"Motor de BD no soportado: {dbType}")
        };
    }

    private static string NormalizeHanaConnStr(string connStr)
    {
        foreach (var (fqdn, ip) in _hanaHostAliases)
            connStr = connStr.Replace(fqdn, ip, StringComparison.OrdinalIgnoreCase);
        return connStr;
    }

    private static string NormalizeSqlConnStr(string connStr)
    {
        // Microsoft.Data.SqlClient 4+ cifra por defecto; el servidor no tiene cert de confianza.
        if (!connStr.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            connStr = connStr.TrimEnd(';') + ";TrustServerCertificate=True;";
        return connStr;
    }
}
