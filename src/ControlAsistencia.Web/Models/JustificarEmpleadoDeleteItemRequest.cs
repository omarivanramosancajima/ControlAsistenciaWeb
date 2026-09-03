namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoDeleteItemRequest
{
    public int LeaveId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
}
