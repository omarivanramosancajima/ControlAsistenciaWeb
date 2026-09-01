namespace ControlAsistencia.Web.Models;

/// <summary>
/// Identidad y contexto organizacional de una persona procesada por el motor.
/// </summary>
public class AttendancePersonContext
{
    public int PersonId { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string? PersonDocumentNumber { get; set; }
    public string? PersonName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string CompanyTaxId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int? CompanyDepartmentId { get; set; }
    public string? CompanyResolutionDiagnostic { get; set; }
}
