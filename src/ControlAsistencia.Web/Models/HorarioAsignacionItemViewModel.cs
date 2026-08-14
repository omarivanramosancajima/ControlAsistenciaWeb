namespace ControlAsistencia.Web.Models;

public class HorarioAsignacionItemViewModel
{
    public int SchClassId { get; set; }
    public string SchName { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Color { get; set; }
}