using WmsPortal.Core.Models;

namespace WmsPortal.Data.QueryBuilders;

/// <summary>
/// Genera SQL adaptado al motor de BD (HANA o SQL Server).
/// Diferencias clave:
///   HANA:       ADD_DAYS(CURRENT_DATE, -N)   |  "Schema"."Tabla"  |  LIMIT/OFFSET
///   SQL Server: DATEADD(DAY,-N,GETDATE())     |  [Schema].[Tabla]  |  OFFSET/FETCH
/// </summary>
public static class QueryFactory
{
    // ─── Helpers de sintaxis ────────────────────────────────────────────────

    public static string DateAdd(string dbType, int days, string dateExpr = "")
    {
        if (dbType == "HANA")
            return days == 0 ? "CURRENT_DATE" : $"ADD_DAYS(CURRENT_DATE, {days})";

        var base_ = string.IsNullOrEmpty(dateExpr) ? "CAST(GETDATE() AS DATE)" : dateExpr;
        return days == 0 ? base_ : $"DATEADD(DAY, {days}, {base_})";
    }

    public static string TableRef(string dbType, string schema, string table)
    {
        return dbType == "HANA"
            ? $"\"{schema}\".\"{table}\""
            : $"[{schema}].[dbo].[{table}]";
    }

    public static string Col(string dbType, string column)
    {
        return dbType == "HANA" ? $"\"{column}\"" : $"[{column}]";
    }

    public static string Pagination(string dbType, int page, int pageSize)
    {
        int offset = (page - 1) * pageSize;
        return dbType == "HANA"
            ? $"LIMIT {pageSize} OFFSET {offset}"
            : $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
    }

    // ─── Queries de transacciones ───────────────────────────────────────────

    public static (string dataSql, string countSql) BuildTransaccionQuery(
        string dbType, string schema, TipoTransaccion tipo,
        string? status, string? numDoc, string? lpn, string? customerPo,
        DateTime? desde, DateTime? hasta,
        int page, int pageSize)
    {
        string C(string col) => Col(dbType, col);
        string T(string tbl) => TableRef(dbType, schema, tbl);

        var (selectCols, tableName, whereExtra, orderBy, groupBy) = tipo switch
        {
            TipoTransaccion.EnvioProducto => (
                $"DISTINCT {C("CreatedAt")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("part_a")}",
                "STG_SAP_PRODUCTS",
                "",
                C("Sync_Date"),
                ""
            ),
            TipoTransaccion.EnvioSucursal => (
                $"DISTINCT {C("CreatedAt")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("code")},{C("address_3")} AS {C("Name")},{C("address_1")},{C("cust_field_1")} AS {C("Id_Suc")}",
                "STG_SAP_STORE",
                "",
                C("Sync_Date"),
                ""
            ),
            TipoTransaccion.EnvioOrdenes => (
                $"DISTINCT {C("CreatedAt")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("cust_field_5")} AS {C("Tipo_Doc")},{C("order_nbr")},{C("customer_po_nbr")},{C("cust_field_3")} AS {C("Name")}",
                "STG_SAP_ORDER_HDR",
                "",
                C("Sync_Date"),
                ""
            ),
            TipoTransaccion.EnvioIngresoAsn => (
                $"{C("CreatedAt")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("cust_field_5")} AS {C("Tipo_Doc")},{C("shipment_nbr")}",
                "STG_SAP_IB_SHIPMENT_HDR",
                "",
                C("Sync_Date"),
                ""
            ),
            TipoTransaccion.ConfirmacionAsn => (
                $"{C("TimeStamp")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("shipment_hdr_cust_field_5")} AS {C("Tipo_Doc")},{C("shipment_nbr")},SUM(CAST({C("shipped_qty")} AS INT)) AS shipped_qty_sum,SUM(CAST({C("received_qty")} AS INT)) AS received_qty_sum",
                "STG_WMS_SVSH",
                "",
                C("TimeStamp"),
                $"GROUP BY {C("TimeStamp")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("shipment_nbr")},{C("shipment_hdr_cust_field_5")}"
            ),
            TipoTransaccion.ConfirmacionOrdenes => (
                $"{C("TimeStamp")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("order_hdr_cust_field_5")} AS {C("Tipo_Doc")},{C("order_nbr")},{C("ob_lpn_nbr")},{C("customer_po_nbr")},SUM(CAST({C("shipped_qty")} AS INT)) AS shipped_qty_sum,{C("order_hdr_cust_field_3")} AS {C("Name")}",
                "STG_WMS_SLSH",
                "",
                C("TimeStamp"),
                $"GROUP BY {C("TimeStamp")},{C("Status")},{C("Retry_Count")},{C("Sync_Date")},{C("Sync_Error_Log")},{C("order_nbr")},{C("ob_lpn_nbr")},{C("order_hdr_cust_field_5")},{C("customer_po_nbr")},{C("order_hdr_cust_field_3")}"
            ),
            _ => throw new NotSupportedException()
        };

        string dateField = tipo is TipoTransaccion.ConfirmacionAsn or TipoTransaccion.ConfirmacionOrdenes
            ? C("TimeStamp") : C("CreatedAt");

        string? desdeSql = desde.HasValue
            ? (dbType == "HANA"
                ? $"TO_TIMESTAMP('{desde:yyyy-MM-dd} 00:00:00', 'YYYY-MM-DD HH24:MI:SS')"
                : $"'{desde:yyyy-MM-dd}'")
            : null;

        string? hastaSql = hasta.HasValue
            ? (dbType == "HANA"
                ? $"TO_TIMESTAMP('{hasta:yyyy-MM-dd} 23:59:59.999', 'YYYY-MM-DD HH24:MI:SS.FF3')"
                : $"'{hasta:yyyy-MM-dd} 23:59:59'")
            : null;

        var conditions = new List<string>();
        if (desdeSql != null)
            conditions.Add($"{dateField} >= {desdeSql}");

        if (hastaSql != null)
            conditions.Add($"{dateField} <= {hastaSql}");

        if (!string.IsNullOrWhiteSpace(whereExtra))
        {
            var extra = whereExtra.Trim();
            if (extra.StartsWith("AND ", StringComparison.OrdinalIgnoreCase))
                extra = extra[4..].TrimStart();
            conditions.Add(extra);
        }

        if (!string.IsNullOrWhiteSpace(status))
            conditions.Add($"{C("Status")} = '{status}'");

        if (!string.IsNullOrWhiteSpace(numDoc))
        {
            string docField = tipo switch
            {
                TipoTransaccion.EnvioProducto => C("part_a"),
                TipoTransaccion.EnvioSucursal => C("code"),
                TipoTransaccion.EnvioOrdenes or TipoTransaccion.ConfirmacionOrdenes => C("order_nbr"),
                TipoTransaccion.EnvioIngresoAsn or TipoTransaccion.ConfirmacionAsn => C("shipment_nbr"),
                _ => C("order_nbr")
            };
            conditions.Add($"{docField} LIKE '%{numDoc}%'");
        }

        if (!string.IsNullOrWhiteSpace(lpn) && tipo is TipoTransaccion.ConfirmacionOrdenes)
            conditions.Add($"{C("ob_lpn_nbr")} LIKE '%{lpn}%'");

        if (!string.IsNullOrWhiteSpace(customerPo) && tipo is TipoTransaccion.EnvioOrdenes or TipoTransaccion.ConfirmacionOrdenes)
            conditions.Add($"{C("customer_po_nbr")} LIKE '%{customerPo}%'");

        string where = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        string tbl = T(tableName);
        string orderDir = "DESC";

        if (dbType == "HANA")
        {
            int offset = (page - 1) * pageSize;
            string dataSql = $"SELECT {selectCols} FROM {tbl} {where} {groupBy} ORDER BY {orderBy} {orderDir} LIMIT {pageSize} OFFSET {offset}";
            string countSql = $"SELECT COUNT(*) FROM (SELECT {selectCols} FROM {tbl} {where} {groupBy}) AS T";
            return (dataSql, countSql);
        }
        else
        {
            int offset = (page - 1) * pageSize;
            // SQL Server requiere ORDER BY antes de OFFSET
            string dataSql = $"SELECT {selectCols} FROM {tbl} {where} {groupBy} ORDER BY {orderBy} {orderDir} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            string countSql = $"SELECT COUNT(*) FROM (SELECT {selectCols} FROM {tbl} {where} {groupBy}) AS T";
            return (dataSql, countSql);
        }
    }

    // ─── Query de reset ─────────────────────────────────────────────────────

    public static string BuildResetQuery(string dbType, string schema, TipoTransaccion tipo, int count)
    {
        string C(string col) => Col(dbType, col);

        string tableName = TipoTransaccionInfo.Meta[tipo].TableName;
        string pkField = TipoTransaccionInfo.Meta[tipo].PkField;
        string tbl = TableRef(dbType, schema, tableName);

        string paramList = string.Join(",", Enumerable.Range(0, count).Select(i =>
            dbType == "HANA" ? $"'{{{i}}}'" : $"@p{i}"));

        return $@"UPDATE {tbl}
        SET {C("Status")} = 'PENDIENTE',
            {C("Retry_Count")} = 0,
            {C("Sync_Error_Log")} = NULL
        WHERE {C(pkField)} IN ({paramList})";
    }

    // ─── Queries de dashboard ───────────────────────────────────────────────

    public static string BuildDashboardCountQuery(string dbType, string schema, string tableName,
        string dateField, string excludeStatus, int dias, string[]? groupByCols = null)
    {
        string C(string col) => Col(dbType, col);
        string tbl = TableRef(dbType, schema, tableName);
        // dias=1 → hoy (CURRENT_DATE), dias=7 → hace 6 días, dias=30 → hace 29 días
        string dateFilter = DateAdd(dbType, 1 - dias);

        string whereExclude = string.IsNullOrEmpty(excludeStatus)
            ? "" : $"AND {C("Status")} != '{excludeStatus}'";

        if (groupByCols == null || groupByCols.Length == 0)
        {
            return $@"SELECT {C("Status")}, COUNT(*) AS cnt
FROM {tbl}
WHERE {C(dateField)} >= {dateFilter}
{whereExclude}
GROUP BY {C("Status")}";
        }

        // Para tipos con múltiples filas por registro lógico (Confirmación):
        // primero colapsar con GROUP BY y luego contar por Status
        string innerGroup = string.Join(",", groupByCols.Select(C));
        return $@"SELECT {C("Status")}, COUNT(*) AS cnt FROM (
    SELECT {innerGroup}
    FROM {tbl}
    WHERE {C(dateField)} >= {dateFilter}
    {whereExclude}
    GROUP BY {innerGroup}
) AS T GROUP BY {C("Status")}";
    }

    // ─── Queries de usuarios (siempre en HANA como BD maestra) ─────────────

    public static string InsertUser(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_USERS");
        string now = dbType == "HANA" ? "CURRENT_TIMESTAMP" : "GETDATE()";
        return $"INSERT INTO {tbl} ({Col(dbType,"USERNAME")},{Col(dbType,"EMAIL")},{Col(dbType,"PASSWORD_HASH")},{Col(dbType,"ROLE")},{Col(dbType,"IS_ACTIVE")},{Col(dbType,"CREATED_AT")}) VALUES(@username,@email,@pwdhash,@role,@isActive,{now})";
    }

    public static string UpdateUser(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_USERS");
        return $"UPDATE {tbl} SET {Col(dbType,"EMAIL")}=@email,{Col(dbType,"ROLE")}=@role,{Col(dbType,"IS_ACTIVE")}=@isActive WHERE {Col(dbType,"ID")}=@userId";
    }

    public static string UpdateUserPassword(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_USERS");
        return $"UPDATE {tbl} SET {Col(dbType,"PASSWORD_HASH")}=@pwdhash WHERE {Col(dbType,"ID")}=@userId";
    }

    public static string InsertUserCompany(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_USER_COMPANIES");
        return $"INSERT INTO {tbl} ({Col(dbType,"USER_ID")},{Col(dbType,"COMPANY_ID")},{Col(dbType,"CAN_RESET")}) VALUES(@userId,@companyId,1)";
    }

    public static string DeleteUserCompanies(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_USER_COMPANIES");
        return $"DELETE FROM {tbl} WHERE {Col(dbType,"USER_ID")}=@userId";
    }

    public static string UpdateCompany(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_COMPANIES");
        return $"UPDATE {tbl} SET {Col(dbType,"NAME")}=@cname,{Col(dbType,"DB_TYPE")}=@cdbType,{Col(dbType,"CONNECTION_STR")}=@connStr,{Col(dbType,"SCHEMA_NAME")}=@schemaName,{Col(dbType,"SHOW_IN_LOGIN")}=@showInLogin WHERE {Col(dbType,"ID")}=@compId";
    }

    public static string InsertCompany(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_COMPANIES");
        string now = dbType == "HANA" ? "CURRENT_TIMESTAMP" : "GETDATE()";
        return $"INSERT INTO {tbl} ({Col(dbType,"CODE")},{Col(dbType,"NAME")},{Col(dbType,"DB_TYPE")},{Col(dbType,"CONNECTION_STR")},{Col(dbType,"SCHEMA_NAME")},{Col(dbType,"IS_ACTIVE")},{Col(dbType,"SORT_ORDER")},{Col(dbType,"SHOW_IN_LOGIN")},{Col(dbType,"CREATED_AT")}) VALUES(@code,@cname,@cdbType,@connStr,@schemaName,1,@sortOrder,@showInLogin,{now})";
    }

    public static string GetLoginCompanies(string dbType, string schema) =>
        $"SELECT * FROM {TableRef(dbType, schema, "PORTAL_COMPANIES")} WHERE {Col(dbType,"IS_ACTIVE")} = 1 AND {Col(dbType,"SHOW_IN_LOGIN")} = 1 ORDER BY {Col(dbType,"SORT_ORDER")}";

    public static string GetUserByUsername(string dbType, string schema) =>
        $"SELECT * FROM {TableRef(dbType, schema, "PORTAL_USERS")} WHERE {Col(dbType, "USERNAME")} = @username AND {Col(dbType, "IS_ACTIVE")} = 1";

    public static string GetUserCompanies(string dbType, string schema) =>
        $@"SELECT uc.{Col(dbType, "COMPANY_ID")}, uc.{Col(dbType, "CAN_RESET")},
              c.{Col(dbType, "NAME")} AS CompanyName,
              c.{Col(dbType, "CODE")} AS CompanyCode,
              c.{Col(dbType, "DB_TYPE")} AS DbType
         FROM {TableRef(dbType, schema, "PORTAL_USER_COMPANIES")} uc
         INNER JOIN {TableRef(dbType, schema, "PORTAL_COMPANIES")} c ON c.{Col(dbType, "ID")} = uc.{Col(dbType, "COMPANY_ID")}
         WHERE uc.{Col(dbType, "USER_ID")} = @userId AND c.{Col(dbType, "IS_ACTIVE")} = 1";

    public static string GetAllCompanies(string dbType, string schema) =>
        $"SELECT * FROM {TableRef(dbType, schema, "PORTAL_COMPANIES")} WHERE {Col(dbType, "IS_ACTIVE")} = 1 ORDER BY {Col(dbType, "SORT_ORDER")}";

    public static string InsertAuditLog(string dbType, string schema)
    {
        string tbl = TableRef(dbType, schema, "PORTAL_AUDIT_LOG");
        return dbType == "HANA"
            ? $@"INSERT INTO {tbl} ({Col(dbType,"USER_ID")},{Col(dbType,"COMPANY_ID")},{Col(dbType,"ACTION")},{Col(dbType,"TABLE_NAME")},{Col(dbType,"RECORD_KEY")},{Col(dbType,"OLD_STATUS")},{Col(dbType,"NEW_STATUS")},{Col(dbType,"IP_ADDRESS")})
                 VALUES(:userId,:companyId,:action,:tableName,:recordKey,:oldStatus,:newStatus,:ipAddress)"
            : $@"INSERT INTO {tbl} ([USER_ID],[COMPANY_ID],[ACTION],[TABLE_NAME],[RECORD_KEY],[OLD_STATUS],[NEW_STATUS],[IP_ADDRESS])
                 VALUES(@userId,@companyId,@action,@tableName,@recordKey,@oldStatus,@newStatus,@ipAddress)";
    }
}
