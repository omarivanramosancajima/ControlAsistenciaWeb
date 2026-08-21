namespace ControlAsistencia.Web.Models;

public class AttendanceReportCompanyInfo
{
    public string TaxId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}