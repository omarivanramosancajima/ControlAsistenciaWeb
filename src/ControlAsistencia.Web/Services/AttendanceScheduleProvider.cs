using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendanceScheduleProvider : IAttendanceScheduleProvider
{
    private readonly string _connectionString;

    public AttendanceScheduleProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<AttendanceSchedule?> GetScheduleAsync(int personId, DateOnly date)
    {
        const string assignmentSql = @"
SELECT TOP (1)
    UOR.NUM_OF_RUN_ID AS ShiftId,
    NR.NAME AS ShiftName,
    NR.CYLE AS Cycle,
    NR.UNITS AS Units,
    UOR.STARTDATE AS ShiftAssignmentStartDateTime,
    UOR.ENDDATE AS ShiftAssignmentEndDateTime
FROM dbo.USER_OF_RUN UOR WITH (NOLOCK)
INNER JOIN dbo.NUM_RUN NR WITH (NOLOCK) ON NR.NUM_RUNID = UOR.NUM_OF_RUN_ID
WHERE UOR.USERID = @PersonId
  AND CAST(UOR.STARTDATE AS date) <= @TargetDate
  AND CAST(UOR.ENDDATE AS date) >= @TargetDate
ORDER BY UOR.STARTDATE DESC, UOR.ENDDATE DESC, UOR.ORDER_RUN DESC;";

        const string detailSql = @"
SELECT TOP (1)
    D.SCHCLASSID AS ScheduleClassId,
    S.SCHNAME AS ScheduleName,
    D.STARTTIME AS ScheduledStartDateTime,
    D.ENDTIME AS ScheduledEndDateTime,
    S.CheckInTime1 AS CheckInTime1DateTime,
    S.CheckInTime2 AS CheckInTime2DateTime,
    S.CheckOutTime1 AS CheckOutTime1DateTime,
    S.CheckOutTime2 AS CheckOutTime2DateTime,
    D.SDAYS AS StartDayOffset,
    D.EDAYS AS EndDayOffset,
    S.LateMinutes AS LateToleranceMinutes,
    S.EarlyMinutes AS EarlyToleranceMinutes,
    S.CheckIn AS CheckInMode,
    S.CheckOut AS CheckOutMode
FROM dbo.NUM_RUN_DEIL D WITH (NOLOCK)
LEFT JOIN dbo.SchClass S WITH (NOLOCK) ON S.schClassid = D.SCHCLASSID
WHERE D.NUM_RUNID = @ShiftId
  AND D.SDAYS = @ScheduleDay
ORDER BY D.SDAYS ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var assignment = await connection.QueryFirstOrDefaultAsync<ScheduleAssignmentRow>(assignmentSql, new
            {
                PersonId = personId,
                TargetDate = date.ToDateTime(TimeOnly.MinValue).Date
            });

            if (assignment is null)
            {
                return null;
            }

            var scheduleDay = ResolveScheduleDay(date, assignment.ShiftAssignmentStartDateTime, assignment.Units, assignment.Cycle);
            var detail = await connection.QueryFirstOrDefaultAsync<ScheduleDetailRow>(detailSql, new
            {
                assignment.ShiftId,
                ScheduleDay = scheduleDay
            });

            return new AttendanceSchedule
            {
                ShiftId = assignment.ShiftId,
                ShiftName = assignment.ShiftName,
                Cycle = assignment.Cycle,
                Units = assignment.Units,
                ShiftAssignmentStartDate = DateOnly.FromDateTime(assignment.ShiftAssignmentStartDateTime.Date),
                ShiftAssignmentEndDate = DateOnly.FromDateTime(assignment.ShiftAssignmentEndDateTime.Date),
                ScheduleClassId = detail?.ScheduleClassId,
                ScheduleName = detail?.ScheduleName,
                ScheduledStartTime = detail?.ScheduledStartDateTime is DateTime start ? TimeOnly.FromDateTime(start) : null,
                ScheduledEndTime = detail?.ScheduledEndDateTime is DateTime end ? TimeOnly.FromDateTime(end) : null,
                CheckInTime1 = detail?.CheckInTime1DateTime is DateTime checkInTime1 ? TimeOnly.FromDateTime(checkInTime1) : null,
                CheckInTime2 = detail?.CheckInTime2DateTime is DateTime checkInTime2 ? TimeOnly.FromDateTime(checkInTime2) : null,
                CheckOutTime1 = detail?.CheckOutTime1DateTime is DateTime checkOutTime1 ? TimeOnly.FromDateTime(checkOutTime1) : null,
                CheckOutTime2 = detail?.CheckOutTime2DateTime is DateTime checkOutTime2 ? TimeOnly.FromDateTime(checkOutTime2) : null,
                StartDayOffset = detail?.StartDayOffset,
                EndDayOffset = detail?.EndDayOffset,
                IsOvernight = detail is not null && detail.EndDayOffset > detail.StartDayOffset,
                LateToleranceMinutes = detail?.LateToleranceMinutes,
                EarlyToleranceMinutes = detail?.EarlyToleranceMinutes,
                CheckInMode = detail?.CheckInMode,
                CheckOutMode = detail?.CheckOutMode,
                HasSchedule = detail is not null && detail.ScheduleClassId.HasValue
            };
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener el horario de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener el horario de asistencia.", ex);
        }
    }

    private static int ResolveScheduleDay(DateOnly targetDate, DateTime assignmentStartDate, short? units, short? cycle)
    {
        var totalDays = TurnoCycleDayHelper.GetTotalDays(units ?? -1, cycle ?? 0);
        if (totalDays <= 0)
        {
            return 1;
        }

        var daysFromStart = targetDate.DayNumber - DateOnly.FromDateTime(assignmentStartDate.Date).DayNumber;
        if (daysFromStart < 0)
        {
            return 1;
        }

        return (daysFromStart % totalDays) + 1;
    }

    private sealed class ScheduleAssignmentRow
    {
        public int ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public short? Cycle { get; set; }
        public short? Units { get; set; }
        public DateTime ShiftAssignmentStartDateTime { get; set; }
        public DateTime ShiftAssignmentEndDateTime { get; set; }
    }

    private sealed class ScheduleDetailRow
    {
        public int? ScheduleClassId { get; set; }
        public string? ScheduleName { get; set; }
        public DateTime? ScheduledStartDateTime { get; set; }
        public DateTime? ScheduledEndDateTime { get; set; }
        public DateTime? CheckInTime1DateTime { get; set; }
        public DateTime? CheckInTime2DateTime { get; set; }
        public DateTime? CheckOutTime1DateTime { get; set; }
        public DateTime? CheckOutTime2DateTime { get; set; }
        public short? StartDayOffset { get; set; }
        public short? EndDayOffset { get; set; }
        public int? LateToleranceMinutes { get; set; }
        public int? EarlyToleranceMinutes { get; set; }
        public int? CheckInMode { get; set; }
        public int? CheckOutMode { get; set; }
    }
}