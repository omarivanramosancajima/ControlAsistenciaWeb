namespace ControlAsistencia.Web.Models;

/// <summary>
/// [ASISTWEB][SEC.01]
/// [ASISTWEB][SEC.02]
/// [ASISTWEB][SEC.03.06.06]
/// [ASISTWEB][SEC.04]
/// [ASISTWEB][SEC.05]
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
    public string CompanyTaxId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyResolutionDiagnostic { get; set; }
    public DateOnly Date { get; set; }
    public string DayNumberText { get; set; } = string.Empty;
    public string DayNameText { get; set; } = string.Empty;
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
    public bool HasScheduledAssignment { get; set; }
    public bool HasExceptions { get; set; }
    public string ExceptionDisplayText { get; set; } = string.Empty;
    public string ScheduleDisplayText { get; set; } = string.Empty;
    public AttendancePersonAccumulation Accumulation { get; set; } = new();
    public string ScheduleObservationCode { get; set; } = string.Empty;
    public bool ProcessedBySection02 { get; set; }
    public bool ProcessedBySection03 { get; set; }

    /// <summary>
    /// Derivación final de inconsistencias restringida a los tres casos aprobados.
    /// </summary>
    public AttendanceInconsistencyKind InconsistencyKind { get; set; }
}