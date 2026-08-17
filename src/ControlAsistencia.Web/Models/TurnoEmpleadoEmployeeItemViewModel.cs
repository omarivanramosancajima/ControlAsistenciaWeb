namespace ControlAsistencia.Web.Models;

public class TurnoEmpleadoEmployeeItemViewModel
{
    public int UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string? Ssn { get; set; }
    public string? Name { get; set; }
    public int? DefaultDeptId { get; set; }
    public string? DepartmentName { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoBase64 { get; set; }
    public int? Privilege { get; set; }
    public string PrivilegeDescription { get; set; } = "Desconocido";
}