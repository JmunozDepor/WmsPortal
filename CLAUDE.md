# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (dev server at https://localhost:46790)
dotnet run --project WmsPortal.Web/WmsPortal.Web.csproj

# Publish
dotnet publish -c Release
```

There are no test projects.

## Architecture

Three-project solution targeting .NET 8:

- **WmsPortal.Core** — interfaces (`IAuthService`, `ITransaccionService`, `IDashboardService`, `IAuditService`) and domain models (`PortalUser`, `PortalCompany`, `TransaccionRow`, `TipoTransaccion`, etc.). No dependencies on other projects.
- **WmsPortal.Data** — Dapper-based repositories (`TransaccionRepository`, `UserRepository`), `DbConnectionFactory` (creates SAP HANA or SQL Server connections), and `QueryFactory` (generates database-specific SQL).
- **WmsPortal.Web** — ASP.NET Core MVC: controllers, Razor views, and service implementations that use the Core interfaces and Data repositories. Session-based auth with 8-hour timeout.

### Master/Tenant database model

There are two database tiers:

- **Master database** (always SAP HANA): Portal metadata — `PORTAL_USERS`, `PORTAL_COMPANIES`, `PORTAL_USER_COMPANIES`, `PORTAL_AUDIT_LOG`. Connection configured in `appsettings.json` under `Master`.
- **Tenant databases** (HANA or SQL Server, one per company): WMS staging tables for SAP integration (e.g. `STG_SAP_PRODUCTS`, `STG_WMS_SVSH`). Each `PortalCompany` carries its own `DbType` + `ConnStr`.

### Database abstraction

`QueryFactory` (in `WmsPortal.Data`) generates all SQL. It switches syntax based on `DbType`:

| Concern | HANA | SQL Server |
|---|---|---|
| Identifiers | `"Schema"."Table"` | `[Schema].[Table]` |
| Pagination | `LIMIT n OFFSET m` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` |
| Parameters | positional | named (`@param`) |
| Dates | `ADD_DAYS(CURRENT_DATE, -N)` | `DATEADD(DAY, -N, GETDATE())` |

When adding a new query, add both HANA and SQL Server branches in `QueryFactory`.

### Transaction types

`TipoTransaccion` enum (in Core) has six values: `EnvioProducto`, `EnvioSucursal`, `EnvioOrdenes`, `EnvioIngresoAsn`, `ConfirmacionAsn`, `ConfirmacionOrdenes`. Adding a new type requires updates in: `TipoTransaccionInfo.Meta` (Core), `QueryFactory` (Data), `GetColumnsForTipoAsync` + `MapRow` (TransaccionService), and the corresponding Razor view column rendering.

### Dynamic row mapping

`TransaccionRepository.GetPagedAsync` returns `IEnumerable<dynamic>` (Dapper). `TransaccionService.MapRow` casts each row to `IDictionary<string, object>` and does case-insensitive field lookups (e.g. `"Sync_Date"` or `"SYNC_DATE"`) to handle HANA vs SQL Server column casing differences. When assigning the mapped list to `PagedResult<TransaccionRow>.Items`, always cast explicitly: `rows.Select(r => (TransaccionRow)MapRow(r, tipo)).ToList()` — without the cast, the LINQ expression infers `List<dynamic>`.

### Auth and roles

`AuthService` validates credentials against the master DB using BCrypt (cost 11). Roles: `Admin` (full access, all companies), `Operador` (can reset transactions), `Viewer` (read-only). `SessionData` (JSON-serialized into HTTP session) carries the active user, role, and selected company context.

### Front-end

Vanilla JS with no framework. CSS uses custom design tokens (no Bootstrap). Chart.js loaded from CDN for the dashboard. Toast notifications and modal helpers live in `wwwroot/js/site.js`.

## Configuration

`WmsPortal.Web/appsettings.json` requires a working master HANA connection:

```json
{
  "Master": {
    "DbType": "HANA",
    "ConnStr": "Server=<host>:39015;UserID=<user>;Password=<password>;",
    "Schema": "CLPRD_WMS"
  }
}
```

There is no migration tooling — the database schema is pre-existing (legacy SAP environment). Do not use EF Core or any ORM other than Dapper.
