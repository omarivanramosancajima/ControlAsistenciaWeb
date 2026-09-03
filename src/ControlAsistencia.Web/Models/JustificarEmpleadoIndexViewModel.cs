namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoIndexViewModel
{
    public IReadOnlyList<DepartmentDTO> Departments { get; set; } = [];
    public IReadOnlyList<JustificarEmpleadoExcepcionDisponibleViewModel> Excepciones { get; set; } = [];
}
