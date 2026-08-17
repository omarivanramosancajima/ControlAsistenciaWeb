namespace ControlAsistencia.Web.Models;

public class TurnosEmpleadoAssignResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<TurnosEmpleadoProgressItemViewModel> ProgressItems { get; set; } = [];
}