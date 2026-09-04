namespace ControlAsistencia.Web.Models;

public class AccesoAlSistemaNewViewModel
{
    public IReadOnlyList<DepartmentDTO> Departments { get; set; } = Array.Empty<DepartmentDTO>();
    public IReadOnlyList<AccesoAlSistemaEmployeeItemViewModel> Employees { get; set; } = Array.Empty<AccesoAlSistemaEmployeeItemViewModel>();
    public int SelectedDeptId { get; set; }
    public int? SelectedUserId { get; set; }
    public AccesoAlSistemaEmployeeItemViewModel? SelectedEmployee { get; set; }
}
