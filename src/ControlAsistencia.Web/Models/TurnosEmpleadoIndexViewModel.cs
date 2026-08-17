namespace ControlAsistencia.Web.Models;

public class TurnosEmpleadoIndexViewModel
{
    public IReadOnlyList<DepartmentDTO> Departments { get; set; } = [];
    public IReadOnlyList<TurnoDTO> Turnos { get; set; } = [];
}