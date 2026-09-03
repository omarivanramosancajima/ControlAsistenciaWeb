namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoExcepcionAsignacionViewModel
{
    public int UserId { get; set; }
    public int LeaveId { get; set; }
    public string LeaveName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Reason { get; set; }
    public short Unit { get; set; }
    public short Classify { get; set; }
    public string ReportSymbol { get; set; } = string.Empty;
    public DateTime? RegisteredAt { get; set; }
    public string UnitText => Unit switch
    {
        1 => "Horas",
        2 => "Minutos",
        3 => "Días",
        _ => "Desconocida"
    };
    public string ClassifyText => Classify switch
    {
        0 => "Normal",
        128 => "Canje",
        _ => "Desconocida"
    };
}
