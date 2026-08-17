namespace ControlAsistencia.Web.Models;

public class TurnosEmpleadoDeleteConfirmViewModel
{
    public int UserId { get; set; }
    public string? Ssn { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int NumOfRunId { get; set; }
    public string TurnoName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}