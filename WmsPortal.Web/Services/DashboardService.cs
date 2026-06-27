using WmsPortal.Core.Interfaces;
using WmsPortal.Core.Models;
using WmsPortal.Data.Repositories;

namespace WmsPortal.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly TransaccionRepository _repo;

    public DashboardService(TransaccionRepository repo)
    {
        _repo = repo;
    }

    public async Task<DashboardResumen> GetResumenAsync(PortalCompany company, int dias)
    {
        var resumen = new DashboardResumen();

        // Configuración de cada tipo: tabla, campo de fecha, estado a excluir
        static string[] NoGroup() => Array.Empty<string>();
        static string[] AsnGroup() => ["TimeStamp","Status","Retry_Count","Sync_Date","Sync_Error_Log","shipment_nbr","shipment_hdr_cust_field_5"];
        static string[] OrdGroup() => ["TimeStamp","Status","Retry_Count","Sync_Date","Sync_Error_Log","order_nbr","ob_lpn_nbr","order_hdr_cust_field_5","customer_po_nbr","order_hdr_cust_field_3"];

        var configs = new[]
        {
            (tipo: TipoTransaccion.EnvioProducto,       tabla: "STG_SAP_PRODUCTS",         dateField: "CreatedAt",  excluye: "", groupBy: NoGroup()),
            (tipo: TipoTransaccion.EnvioSucursal,       tabla: "STG_SAP_STORE",            dateField: "CreatedAt",  excluye: "", groupBy: NoGroup()),
            (tipo: TipoTransaccion.EnvioOrdenes,        tabla: "STG_SAP_ORDER_HDR",        dateField: "CreatedAt",  excluye: "", groupBy: NoGroup()),
            (tipo: TipoTransaccion.EnvioIngresoAsn,     tabla: "STG_SAP_IB_SHIPMENT_HDR",  dateField: "CreatedAt",  excluye: "", groupBy: NoGroup()),
            (tipo: TipoTransaccion.ConfirmacionAsn,     tabla: "STG_WMS_SVSH",             dateField: "TimeStamp",  excluye: "", groupBy: AsnGroup()),
            (tipo: TipoTransaccion.ConfirmacionOrdenes, tabla: "STG_WMS_SLSH",             dateField: "TimeStamp",  excluye: "", groupBy: OrdGroup()),
        };

        foreach (var cfg in configs)
        {
            var rows = await _repo.GetDashboardCountsAsync(
                company, cfg.tabla, cfg.dateField, cfg.excluye, dias, cfg.groupBy);

            var stat = new EstadisticaTipo
            {
                Tipo = TipoTransaccionInfo.Meta[cfg.tipo].Label
            };

            foreach (var row in rows)
            {
                var d = (IDictionary<string, object>)row;
                string status = d.TryGetValue("Status", out var sv) ? sv?.ToString() ?? "" :
                               d.TryGetValue("STATUS", out var sv2) ? sv2?.ToString() ?? "" : "";
                int cnt = int.TryParse(d.TryGetValue("cnt", out var cv) ? cv?.ToString() :
                                       d.TryGetValue("CNT", out var cv2) ? cv2?.ToString() : "0", out var n) ? n : 0;

                if (status is "SYNC_OK" or "PROCESADO_SAP")
                    stat.Ok += cnt;
                else if (status is "PENDIENTE")
                    stat.Pendiente += cnt;
                else
                    stat.Error += cnt;

            }

            resumen.PorTipo.Add(stat);
            resumen.TotalOk += stat.Ok;
            resumen.TotalError += stat.Error;
            resumen.TotalPendiente += stat.Pendiente;
            resumen.TotalTransacciones += stat.Total;
        }

        return resumen;
    }
}

public class AuditService : IAuditService
{
    private readonly UserRepository _userRepo;

    public AuditService(UserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task LogResetAsync(AuditLog entry, PortalCompany company)
    {
        // El log siempre va a la BD maestra (la misma donde está PORTAL_AUDIT_LOG)
        await _userRepo.InsertAuditAsync(entry);
    }

    public Task<List<AuditLog>> GetAuditLogAsync(PortalCompany company, int page, int pageSize)
        => Task.FromResult(new List<AuditLog>());
}
