using System.Globalization;
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
        // [ASISTWEB][SEC.01.04]
        // [ASISTWEB][SEC.03.01]
        // [ASISTWEB][SEC.03.02]
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
ORDER BY UOR.NUM_OF_RUN_ID ASC;";

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
    D.NUM_RUNID AS NumRunId,
    D.SDAYS AS ScheduleDay,
    D.EDAYS AS ScheduleEndDay,
    D.SDAYS AS StartDayOffset,
    D.EDAYS AS EndDayOffset,
    S.LateMinutes AS LateToleranceMinutes,
    S.EarlyMinutes AS EarlyToleranceMinutes,
    S.CheckIn AS CheckInMode,
    S.CheckOut AS CheckOutMode,
    CASE
        WHEN S.SCHNAME LIKE 'HV%' THEN 45
        WHEN S.SCHNAME LIKE 'HN%' THEN 90
        WHEN S.SCHNAME LIKE 'HR%' THEN 60
        ELSE 0
    END AS BreakMinutes
FROM dbo.NUM_RUN_DEIL D WITH (NOLOCK)
LEFT JOIN dbo.SchClass S WITH (NOLOCK) ON S.schClassid = D.SCHCLASSID
WHERE D.NUM_RUNID = @ShiftId
  AND D.SDAYS = @ScheduleDay
ORDER BY D.SDAYS ASC;";

//  AND CASE WHEN (D.SDAYS >= 1 AND D.SDAYS < 8) THEN D.SDAYS CASE WHEN (D.SDAYS >= 8 AND D.SDAYS < 15) THEN D.SDAYS-7 CASE WHEN (D.SDAYS >= 15 AND D.SDAYS < 22) THEN D.SDAYS-14 CASE WHEN (D.SDAYS >= 22 AND D.SDAYS < 29) THEN D.SDAYS-21 CASE WHEN (D.SDAYS >= 29 AND D.SDAYS < 32) THEN D.SDAYS-28 END = @ScheduleDay

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

            var scheduleDay = ResolveScheduleDay(date, assignment.Units);
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
                NumRunId = detail?.NumRunId,
                ScheduleDay = detail?.ScheduleDay,
                ScheduleEndDay = detail?.ScheduleEndDay,
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
                BreakMinutes = detail?.BreakMinutes ?? 0,
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

    private static int ResolveScheduleDay(DateOnly targetDate, short? units)
    {
        int resolveScheduleDay=0;
        // [ASISTWEB][SEC.01.04]
        // La especificación usa exclusivamente este orden para NUM_RUN_DEIL.SDAYS:
        // Semanal
        // lunes=1, martes=2, miércoles=3, jueves=4, viernes=5, sábado=6, domingo=7.
        if (units == 1)
        {
            // Semanal
            // lunes=1, martes=2, miércoles=3, jueves=4, viernes=5, sábado=6, domingo=7.
            resolveScheduleDay= targetDate.DayOfWeek switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                DayOfWeek.Sunday => 7,
                _ => 0
            };
        }else if (units == 0 || units == 2)
        {
            // Diario o Mensual
            resolveScheduleDay= targetDate.Day;   
        }else if (units == 3)
        {
            //Quincenal nuevo semana par/impar
            // 1. Obtener el número de semana ISO (continuo y estandarizado)
            int numeroSemana = ISOWeek.GetWeekOfYear(targetDate.ToDateTime(TimeOnly.MinValue));

            // 2. Determinar si es Semana Impar o Semana Par
            bool esSemanaImpar = (numeroSemana % 2 != 0); 

            // 3.  lógica de asignación de horarios
            if (esSemanaImpar)
            {
                // Aplicar el horario de la primera semana según el fechaAsistencia.DayOfWeek
                resolveScheduleDay= targetDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => 1,
                    DayOfWeek.Tuesday => 2,
                    DayOfWeek.Wednesday => 3,
                    DayOfWeek.Thursday => 4,
                    DayOfWeek.Friday => 5,
                    DayOfWeek.Saturday => 6,
                    DayOfWeek.Sunday => 7,
                    _ => 0
                };
            }
            else
            {
                // Aplicar el horario de la segunda semana según el fechaAsistencia.DayOfWeek
                resolveScheduleDay= targetDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => 8,
                    DayOfWeek.Tuesday => 9,
                    DayOfWeek.Wednesday => 10,
                    DayOfWeek.Thursday => 11,
                    DayOfWeek.Friday => 12,
                    DayOfWeek.Saturday => 13,
                    DayOfWeek.Sunday => 14,
                    _ => 1
                };
            }            
            // Quincenal logica anterior
            /*
            if (targetDate.Day <= 15)
            {
                resolveScheduleDay= targetDate.Day;   
            }else if (targetDate.Day > 15 && targetDate.Day <= 30)
            {
                resolveScheduleDay= targetDate.Day-15;                  
            }else if (targetDate.Day > 30)
            {
                resolveScheduleDay= targetDate.Day-30;                  
            }
            */
        }
        return resolveScheduleDay;

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
        public int? NumRunId { get; set; }
        public short? ScheduleDay { get; set; }
        public short? ScheduleEndDay { get; set; }
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
        public int? BreakMinutes { get; set; }
    }
}