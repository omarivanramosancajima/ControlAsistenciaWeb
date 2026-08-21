namespace ControlAsistencia.Web.Models;

public class AttendanceReportFilterPerson
{
    public int PersonId { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string? PersonDocumentNumber { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}