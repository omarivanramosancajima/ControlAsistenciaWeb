namespace ControlAsistencia.Web.Models;

public class DepartmentDTO
{
    public int DeptId { get; set; }
    public string? DeptName { get; set; }
    public int SupDeptId { get; set; }
    public int Level { get; set; }
    public string HierarchyName { get; set; } = string.Empty;
}