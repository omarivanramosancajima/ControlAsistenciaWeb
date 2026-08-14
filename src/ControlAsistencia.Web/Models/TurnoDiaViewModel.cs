namespace ControlAsistencia.Web.Models;

public class TurnoDiaViewModel
{
    public int DayNumber { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public int? SchClassId { get; set; }
    public string? SchName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Color { get; set; }
    public bool IsAssigned => SchClassId.HasValue;
}