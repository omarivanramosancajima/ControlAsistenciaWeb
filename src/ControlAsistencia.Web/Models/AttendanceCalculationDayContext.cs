namespace ControlAsistencia.Web.Models;

/// <summary>
/// [ASISTWEB][SEC.00]
/// [ASISTWEB][SEC.01]
/// [ASISTWEB][SEC.01.01]
/// [ASISTWEB][SEC.01.02]
/// [ASISTWEB][SEC.01.03]
/// [ASISTWEB][SEC.01.04]
/// Contexto de entrada para el cálculo de un único día. El orquestador lo prepara antes de entregarlo al motor.
/// Solo transporta datos ya resueltos.
/// </summary>
public class AttendanceCalculationDayContext
{
    public int PersonId { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string? PersonDocumentNumber { get; set; }
    public string? PersonName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string CompanyTaxId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyResolutionDiagnostic { get; set; }
    public DateOnly CalculationDate { get; set; }
    public AttendanceSchedule? Schedule { get; set; }
    public IReadOnlyList<AttendanceMark> Marks { get; set; } = Array.Empty<AttendanceMark>();
    public IReadOnlyList<AttendanceMark> NextDayMarks { get; set; } = Array.Empty<AttendanceMark>();
    public AttendanceCalculationParameters Parameters { get; set; } = new();
    public IReadOnlyList<AttendanceException> Exceptions { get; set; } = Array.Empty<AttendanceException>();
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsNoSchedule { get; set; }
    public bool HasExceptions => Exceptions.Count > 0;
}