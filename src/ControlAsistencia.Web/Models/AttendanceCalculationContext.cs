namespace ControlAsistencia.Web.Models;

/// <summary>
/// Contexto de entrada preparado para el futuro motor de asistencia.
/// Solo transporta datos ya resueltos.
/// </summary>
public class AttendanceCalculationContext
{
    public int PersonId { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string? PersonDocumentNumber { get; set; }
    public string? PersonName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
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
}