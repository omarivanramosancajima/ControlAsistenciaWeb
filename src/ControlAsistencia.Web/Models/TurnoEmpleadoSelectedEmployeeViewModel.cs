namespace ControlAsistencia.Web.Models;

public class TurnoEmpleadoSelectedEmployeeViewModel
{
    public int UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
}