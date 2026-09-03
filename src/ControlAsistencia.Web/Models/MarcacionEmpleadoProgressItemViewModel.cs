namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoProgressItemViewModel
{
    public int Position { get; set; }
    public int Total { get; set; }
    public int UserId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string BadgeNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
}