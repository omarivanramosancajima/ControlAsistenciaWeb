namespace ControlAsistencia.Web.Models;

/// <summary>
/// Resultado de asistencia de una persona para un único día.
/// </summary>
public class AttendanceDayResult
{
    public int PersonId { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string? PersonDocumentNumber { get; set; }
    public string? PersonName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateOnly Date { get; set; }
    public AttendanceSchedule? Schedule { get; set; }
    public AttendanceMark? EntryMark { get; set; }
    public AttendanceMark? ExitMark { get; set; }
    public IReadOnlyList<AttendanceMark> IntermediateMarks { get; set; } = Array.Empty<AttendanceMark>();
    public bool IsAbsent { get; set; }
    public TimeSpan? EffectiveWorkDuration { get; set; }
    public TimeSpan? PresenceDuration { get; set; }
    public TimeSpan? LateEntryDuration { get; set; }
    public TimeSpan? EarlyExitDuration { get; set; }
    public TimeSpan? OvertimeDuration { get; set; }
    public AttendanceException? Exception { get; set; }
    public TimeSpan? JustifiedDuration { get; set; }
    public decimal? JustifiedDayFraction { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHolidayWithSchedule { get; set; }
    public bool IsHolidayWithoutSchedule { get; set; }
    public bool IsNoSchedule { get; set; }

    /// <summary>
    /// Derivación final de inconsistencias restringida a los tres casos aprobados.
    /// </summary>
    public AttendanceInconsistencyKind InconsistencyKind { get; set; }
}