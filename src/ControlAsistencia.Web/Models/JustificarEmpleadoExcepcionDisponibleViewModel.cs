namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoExcepcionDisponibleViewModel
{
    public int LeaveId { get; set; }
    public string LeaveName { get; set; } = string.Empty;
    public short Unit { get; set; }
    public short Classify { get; set; }
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
