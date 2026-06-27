namespace WmsPortal.Core.Models;

// ─── Tipos de transacción ───────────────────────────────────────────────────
public enum TipoTransaccion
{
    EnvioProducto,
    EnvioSucursal,
    EnvioOrdenes,
    EnvioIngresoAsn,
    ConfirmacionAsn,
    ConfirmacionOrdenes
}

public static class TipoTransaccionInfo
{
    public static readonly Dictionary<TipoTransaccion, TransaccionMeta> Meta = new()
    {
        [TipoTransaccion.EnvioProducto]     = new("Envío - Producto",     "STG_SAP_PRODUCTS",        "part_a",       "SAP → WMS", "Producto"),
        [TipoTransaccion.EnvioSucursal]     = new("Envío - Sucursal",     "STG_SAP_STORE",           "code",         "SAP → WMS", "Código Suc."),
        [TipoTransaccion.EnvioOrdenes]      = new("Envío - Órdenes",      "STG_SAP_ORDER_HDR",       "order_nbr",    "SAP → WMS", "N° Orden"),
        [TipoTransaccion.EnvioIngresoAsn]   = new("Envío - Ingreso ASN",  "STG_SAP_IB_SHIPMENT_HDR", "shipment_nbr", "SAP → WMS", "N° Shipment"),
        [TipoTransaccion.ConfirmacionAsn]     = new("Confirmación Ingreso",  "STG_WMS_SVSH",            "shipment_nbr", "WMS → SAP", "N° Shipment"),
        [TipoTransaccion.ConfirmacionOrdenes] = new("Confirmación Ordenes", "STG_WMS_SLSH",           "order_nbr",    "WMS → SAP", "N° Orden"),
    };
}

public record TransaccionMeta(string Label, string TableName, string PkField, string Direction, string DocLabel = "N° Documento");

// ─── Filtros de búsqueda ────────────────────────────────────────────────────
public class TransaccionFilter
{
    public TipoTransaccion Tipo { get; set; } = TipoTransaccion.EnvioProducto;
    public string? Status { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Lpn { get; set; }
    public string? CustomerPo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ─── Resultado paginado ─────────────────────────────────────────────────────
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ─── DTO genérico de transacción (fila del grid) ────────────────────────────
public class TransaccionRow
{
    public string? TimeStamp { get; set; }
    public string? Status { get; set; }
    public int? RetryCount { get; set; }
    public string? SyncDate { get; set; }
    public string? SyncErrorLog { get; set; }
    // Campos específicos (se rellenan según tipo)
    public string? PartA { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Address1 { get; set; }
    public string? IdSuc { get; set; }
    public string? TipoDoc { get; set; }
    public string? OrderNbr { get; set; }
    public string? CustomerPoNbr { get; set; }
    public string? ShipmentNbr { get; set; }
    public string? ObLpnNbr { get; set; }
    public string? ShippedQty { get; set; }
    public string? ReceivedQty { get; set; }
    // Clave para reset
    public string? PkValue { get; set; }
    public string? PkValue2 { get; set; } // Para claves compuestas
}

// ─── Definición de columnas para el grid ───────────────────────────────────
public class ColumnDef
{
    public string Field { get; set; } = "";
    public string Header { get; set; } = "";
    public string Width { get; set; } = "auto";
    public bool IsStatus { get; set; }
    public bool IsError { get; set; }
}

// ─── Dashboard ──────────────────────────────────────────────────────────────
public class DashboardResumen
{
    public int TotalTransacciones { get; set; }
    public int TotalOk { get; set; }
    public int TotalError { get; set; }
    public int TotalPendiente { get; set; }
    public List<EstadisticaTipo> PorTipo { get; set; } = new();
}

public class EstadisticaTipo
{
    public string Tipo { get; set; } = "";
    public int Ok { get; set; }
    public int Error { get; set; }
    public int Pendiente { get; set; }
    public int Total => Ok + Error + Pendiente;
}

// ─── Reset masivo ───────────────────────────────────────────────────────────
public class ResetRequest
{
    public TipoTransaccion Tipo { get; set; }
    public List<string> Claves { get; set; } = new();
}

public class ResetResult
{
    public bool Success { get; set; }
    public int RowsAffected { get; set; }
    public string? ErrorMessage { get; set; }
}
