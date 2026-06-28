namespace WmsPortal.Core.Models;

public class WmsStageRow
{
    public long Id { get; set; }
    public string? TipoDoc { get; set; }
    public string? NombreArchivo { get; set; }
    public string? Estado { get; set; }
    public int? Intentos { get; set; }
    public string? MensajeError { get; set; }
    public string? FechaInserto { get; set; }
    public string? FechaProceso { get; set; }
}

public class WmsStageFilter
{
    public string? Estado { get; set; }
    public string? NombreArchivo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
