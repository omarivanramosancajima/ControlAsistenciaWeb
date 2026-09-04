namespace ControlAsistencia.Web.Models;

public class AreaItemViewModel
{
    public int DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public int SupDeptId { get; set; }
    public int Level { get; set; }
    public bool IsRoot { get; set; }
    public bool HasChildren { get; set; }
    public int EmployeeCount { get; set; }
}
