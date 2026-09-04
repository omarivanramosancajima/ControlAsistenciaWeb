namespace ControlAsistencia.Web.Models;

public class ReubicacionEmpleadoProgressItemViewModel
{
    public int Position { get; set; }
    public int Total { get; set; }
    public int UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendiente";
    public string? Result { get; set; }
    public bool Success { get; set; }
    public string? Detail { get; set; }
}
