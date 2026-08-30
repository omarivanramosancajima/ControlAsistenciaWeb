namespace ControlAsistencia.Web.Models;

/// <summary>
/// [ASISTWEB][SEC.01.04]
/// [ASISTWEB][SEC.03.01]
/// [ASISTWEB][SEC.03.02]
/// Horario efectivo resuelto para una persona en una fecha concreta.
/// </summary>
public class AttendanceSchedule
{
    public int? ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public short? Cycle { get; set; }
    public short? Units { get; set; }
    public DateOnly? ShiftAssignmentStartDate { get; set; }
    public DateOnly? ShiftAssignmentEndDate { get; set; }
    public int? NumRunId { get; set; }
    public short? ScheduleDay { get; set; }
    public short? ScheduleEndDay { get; set; }
    public int? ScheduleClassId { get; set; }
    public string? ScheduleName { get; set; }
    public TimeOnly? ScheduledStartTime { get; set; }
    public TimeOnly? ScheduledEndTime { get; set; }
    public TimeOnly? CheckInTime1 { get; set; }
    public TimeOnly? CheckInTime2 { get; set; }
    public TimeOnly? CheckOutTime1 { get; set; }
    public TimeOnly? CheckOutTime2 { get; set; }
    public short? StartDayOffset { get; set; }
    public short? EndDayOffset { get; set; }
    public bool IsOvernight { get; set; }
    public int? LateToleranceMinutes { get; set; }
    public int? EarlyToleranceMinutes { get; set; }
    public int? CheckInMode { get; set; }
    public int? CheckOutMode { get; set; }
    public int BreakMinutes { get; set; }
    public bool HasSchedule { get; set; }
}