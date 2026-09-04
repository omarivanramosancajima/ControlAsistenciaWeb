namespace ControlAsistencia.Web.Models;

public class AttendanceReportAreaViewModel
{
    public int DeptId { get; set; }
    public string DeptName { get; set; } = string.Empty;
    public int SupDeptId { get; set; }
    public int Level { get; set; }
}
