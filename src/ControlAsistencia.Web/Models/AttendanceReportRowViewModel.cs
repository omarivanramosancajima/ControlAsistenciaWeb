namespace ControlAsistencia.Web.Models;

/// <summary>
/// DATOS DEMO TEMPORALES - NO REPRESENTAN EL MOTOR REAL DE ASISTENCIA.
/// </summary>
public class AttendanceReportRowViewModel
{
    public int Codigo { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Personal { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string HorarioCodigo { get; set; } = string.Empty;
    public string HorarioRango { get; set; } = string.Empty;
    public string HorarioAsignado => $"{HorarioCodigo} {HorarioRango}".Trim();
    public string Entrada { get; set; } = string.Empty;
    public string Salida { get; set; } = string.Empty;
    public string Falta { get; set; } = string.Empty;
    public string HorasEfectivas { get; set; } = string.Empty;
    public string HorasPermiso { get; set; } = string.Empty;
    public string TardanzaEntrada { get; set; } = string.Empty;
    public string SalidaTemprana { get; set; } = string.Empty;
    public string HorasExtras { get; set; } = string.Empty;
    public string Excepcion { get; set; } = string.Empty;
    public string MarcasIntermedias { get; set; } = string.Empty;
}