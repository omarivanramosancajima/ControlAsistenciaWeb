namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoAssignResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<JustificarEmpleadoProgressItemViewModel> ProgressItems { get; set; } = [];
}
