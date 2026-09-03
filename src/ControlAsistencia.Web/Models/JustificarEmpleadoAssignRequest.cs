namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoAssignRequest
{
    public List<int> UserIds { get; set; } = [];
    public int LeaveId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Reason { get; set; }
}
