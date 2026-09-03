namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoDeleteConfirmViewModel
{
    public int UserId { get; set; }
    public string Ssn { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public IReadOnlyList<JustificarEmpleadoExcepcionAsignacionViewModel> Items { get; set; } = [];
}
