namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoDeleteConfirmViewModel
{
    public int UserId { get; set; }
    public string Ssn { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public List<MarcacionEmpleadoMarcacionItemViewModel> Items { get; set; } = [];
}