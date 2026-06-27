using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Data.Repositories;

namespace WmsPortal.Web.Services;

public class TransaccionService : ITransaccionService
{
    private readonly TransaccionRepository _repo;
    private readonly IAuditService _audit;

    public TransaccionService(TransaccionRepository repo, IAuditService audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<PagedResult<TransaccionRow>> GetTransaccionesAsync(
        TransaccionFilter filter, PortalCompany company)
    {
        var (rows, total) = await _repo.GetPagedAsync(company, filter);
        var mapped = rows.Select(r => (TransaccionRow)MapRow(r, filter.Tipo)).ToList();

        return new PagedResult<TransaccionRow>
        {
            Items = mapped,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ResetResult> ResetTransaccionesAsync(
        ResetRequest request, PortalCompany company, int userId, string ipAddress)
    {
        try
        {
            int affected = await _repo.ResetAsync(company, request.Tipo, request.Claves);

            // Auditoría por cada registro reseteado
            foreach (var clave in request.Claves)
            {
                await _audit.LogResetAsync(new AuditLog
                {
                    UserId = userId,
                    CompanyId = company.Id,
                    Action = "RESET_PENDIENTE",
                    TableName = TipoTransaccionInfo.Meta[request.Tipo].TableName,
                    RecordKey = clave,
                    OldStatus = "ERROR",
                    NewStatus = "PENDIENTE",
                    IpAddress = ipAddress
                }, company);
            }

            return new ResetResult { Success = true, RowsAffected = affected };
        }
        catch (Exception ex)
        {
            return new ResetResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public Task<List<ColumnDef>> GetColumnsForTipoAsync(TipoTransaccion tipo)
    {
        var cols = tipo switch
        {
            TipoTransaccion.EnvioProducto => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "PartA",         Header = "Producto",       Width = "120px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            TipoTransaccion.EnvioSucursal => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "Code",          Header = "Código",         Width = "90px" },
                new() { Field = "Name",          Header = "Nombre",         Width = "160px" },
                new() { Field = "Address1",      Header = "Dirección",      Width = "160px" },
                new() { Field = "IdSuc",         Header = "ID Sucursal",    Width = "90px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            TipoTransaccion.EnvioOrdenes => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "TipoDoc",       Header = "Tipo doc",       Width = "100px" },
                new() { Field = "OrderNbr",      Header = "N° Orden",       Width = "110px" },
                new() { Field = "CustomerPoNbr", Header = "PO cliente",     Width = "110px" },
                new() { Field = "Name",          Header = "Cliente",        Width = "150px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            TipoTransaccion.EnvioIngresoAsn => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "TipoDoc",       Header = "Tipo doc",       Width = "100px" },
                new() { Field = "ShipmentNbr",   Header = "N° Shipment",    Width = "120px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            TipoTransaccion.ConfirmacionAsn => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "TipoDoc",       Header = "Tipo doc",       Width = "100px" },
                new() { Field = "ShipmentNbr",   Header = "N° Shipment",    Width = "120px" },
                new() { Field = "ShippedQty",    Header = "Qty enviada",    Width = "90px" },
                new() { Field = "ReceivedQty",   Header = "Qty recibida",   Width = "90px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            TipoTransaccion.ConfirmacionOrdenes => new List<ColumnDef>
            {
                new() { Field = "TimeStamp",    Header = "Fecha/Hora",     Width = "130px" },
                new() { Field = "Status",        Header = "Estado",         Width = "110px", IsStatus = true },
                new() { Field = "RetryCount",    Header = "Reintentos",     Width = "80px" },
                new() { Field = "SyncDate",      Header = "Fecha sync",     Width = "130px" },
                new() { Field = "TipoDoc",       Header = "Tipo doc",       Width = "100px" },
                new() { Field = "OrderNbr",      Header = "N° Orden",       Width = "110px" },
                new() { Field = "ObLpnNbr",      Header = "LPN",            Width = "100px" },
                new() { Field = "CustomerPoNbr", Header = "PO cliente",     Width = "110px" },
                new() { Field = "ShippedQty",    Header = "Qty enviada",    Width = "90px" },
                new() { Field = "Name",          Header = "Cliente",        Width = "150px" },
                new() { Field = "SyncErrorLog",  Header = "Error",          Width = "auto",  IsError = true },
            },
            _ => new List<ColumnDef>()
        };

        return Task.FromResult(cols);
    }

    // ─── Mapper dinámico → TransaccionRow ──────────────────────────────────

    private static TransaccionRow MapRow(dynamic r, TipoTransaccion tipo)
    {
        var d = (IDictionary<string, object>)r;
        string? Get(string key) => d.TryGetValue(key, out var v) ? v?.ToString() : null;

        var row = new TransaccionRow
        {
            TimeStamp    = Get("TimeStamp") ?? Get("TIMESTAMP") ?? Get("CreatedAt") ?? Get("CREATEDAT"),
            Status       = Get("Status")    ?? Get("STATUS"),
            RetryCount   = int.TryParse(Get("Retry_Count") ?? Get("RETRY_COUNT"), out var rc) ? rc : null,
            SyncDate     = Get("Sync_Date") ?? Get("SYNC_DATE"),
            SyncErrorLog = Get("Sync_Error_Log") ?? Get("SYNC_ERROR_LOG"),
        };

        switch (tipo)
        {
            case TipoTransaccion.EnvioProducto:
                row.PartA    = Get("part_a") ?? Get("PART_A");
                row.PkValue  = row.PartA;
                break;
            case TipoTransaccion.EnvioSucursal:
                row.Code     = Get("code")    ?? Get("CODE");
                row.Name     = Get("Name")    ?? Get("NAME");
                row.Address1 = Get("address_1") ?? Get("ADDRESS_1");
                row.IdSuc    = Get("Id_Suc")  ?? Get("ID_SUC");
                row.PkValue  = row.Code;
                break;
            case TipoTransaccion.EnvioOrdenes:
                row.TipoDoc       = Get("Tipo_Doc")       ?? Get("TIPO_DOC");
                row.OrderNbr      = Get("order_nbr")      ?? Get("ORDER_NBR");
                row.CustomerPoNbr = Get("customer_po_nbr") ?? Get("CUSTOMER_PO_NBR");
                row.Name          = Get("Name")            ?? Get("NAME");
                row.PkValue       = row.OrderNbr;
                break;
            case TipoTransaccion.EnvioIngresoAsn:
                row.TipoDoc     = Get("Tipo_Doc")     ?? Get("TIPO_DOC");
                row.ShipmentNbr = Get("shipment_nbr") ?? Get("SHIPMENT_NBR");
                row.PkValue     = row.ShipmentNbr;
                break;
            case TipoTransaccion.ConfirmacionAsn:
                row.TipoDoc     = Get("Tipo_Doc")       ?? Get("TIPO_DOC");
                row.ShipmentNbr = Get("shipment_nbr")   ?? Get("SHIPMENT_NBR");
                row.ShippedQty  = Get("shipped_qty_sum") ?? Get("SHIPPED_QTY_SUM");
                row.ReceivedQty = Get("received_qty_sum") ?? Get("RECEIVED_QTY_SUM");
                row.PkValue     = row.ShipmentNbr;
                row.PkValue2    = row.TimeStamp;
                break;
            case TipoTransaccion.ConfirmacionOrdenes:
                row.TipoDoc       = Get("Tipo_Doc")        ?? Get("TIPO_DOC");
                row.OrderNbr      = Get("order_nbr")       ?? Get("ORDER_NBR");
                row.ObLpnNbr      = Get("ob_lpn_nbr")      ?? Get("OB_LPN_NBR");
                row.CustomerPoNbr = Get("customer_po_nbr") ?? Get("CUSTOMER_PO_NBR");
                row.ShippedQty    = Get("shipped_qty_sum") ?? Get("SHIPPED_QTY_SUM");
                row.Name          = Get("Name")             ?? Get("NAME");
                row.PkValue       = row.OrderNbr;
                row.PkValue2      = row.ObLpnNbr;
                break;
        }

        return row;
    }
}
