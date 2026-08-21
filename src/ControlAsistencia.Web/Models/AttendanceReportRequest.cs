namespace ControlAsistencia.Web.Models;

public class AttendanceReportRequest
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? Persona { get; set; }
    public string? Area { get; set; }
    public string? Estado { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}