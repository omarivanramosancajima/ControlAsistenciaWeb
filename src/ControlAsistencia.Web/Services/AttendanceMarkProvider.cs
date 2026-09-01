using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendanceMarkProvider : IAttendanceMarkProvider
{
    private readonly string _connectionString;

    public AttendanceMarkProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<AttendanceMark>> GetMarksAsync(int personId, DateOnly date)
    {
        const string checkInOutSql = @"
SELECT
    USERID AS PersonId,
    CHECKTIME AS [Timestamp],
    CHECKTYPE AS CheckType,
    VERIFYCODE AS VerifyCode,
    SENSORID AS SensorId,
    sn AS DeviceSerialNumber,
    Memoinfo AS MemoInfo,
    WorkCode AS WorkCode
FROM dbo.CHECKINOUT WITH (NOLOCK)
WHERE USERID = @PersonId
  AND CAST(CHECKTIME AS date) BETWEEN @FromDate AND @ToDate;";

        const string checkExactSql = @"
SELECT
    EXACTID AS RecordId,
    USERID AS PersonId,
    CHECKTIME AS [Timestamp],
    CHECKTYPE AS CheckType,
    ISADD AS IsAdded,
    YUYIN AS Note,
    ISMODIFY AS IsModified,
    ISDELETE AS IsDeleted,
    INCOUNT AS InCount,
    ISCOUNT AS IsCounted,
    MODIFYBY AS ModifiedBy,
    [DATE] AS OperationDate
FROM dbo.CHECKEXACT WITH (NOLOCK)
WHERE USERID = -9999
  AND CAST(CHECKTIME AS date) BETWEEN @FromDate AND @ToDate;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var fromDate = date.ToDateTime(TimeOnly.MinValue).Date;
            var toDate = date.ToDateTime(TimeOnly.MinValue).Date;

            var deviceRows = await connection.QueryAsync<CheckInOutRow>(checkInOutSql, new { PersonId = personId, FromDate = fromDate, ToDate = toDate });
            var manualRows = await connection.QueryAsync<CheckExactRow>(checkExactSql, new { PersonId = personId, FromDate = fromDate, ToDate = toDate });

            var marks = new List<AttendanceMark>();

            marks.AddRange(deviceRows.Select(row => new AttendanceMark
            {
                Source = AttendanceMarkSource.CheckInOut,
                PersonId = row.PersonId,
                Timestamp = row.Timestamp,
                CheckType = row.CheckType,
                MarkType = ResolveMarkType(row.CheckType),
                IsPreviousDayClosureMark = IsPreviousDayClosureMark(row.CheckType),
                VerifyCode = row.VerifyCode,
                SensorId = row.SensorId,
                DeviceSerialNumber = row.DeviceSerialNumber,
                MemoInfo = row.MemoInfo,
                WorkCode = row.WorkCode,
                IsManual = false
            }));

            marks.AddRange(manualRows.Select(row => new AttendanceMark
            {
                Source = AttendanceMarkSource.CheckExact,
                RecordId = row.RecordId,
                PersonId = row.PersonId,
                Timestamp = row.Timestamp,
                CheckType = row.CheckType,
                MarkType = ResolveMarkType(row.CheckType),
                IsPreviousDayClosureMark = IsPreviousDayClosureMark(row.CheckType),
                IsManual = true,
                IsAdded = row.IsAdded,
                IsModified = row.IsModified,
                IsDeleted = row.IsDeleted,
                IsCounted = row.IsCounted,
                InCount = row.InCount,
                Note = row.Note,
                ModifiedBy = row.ModifiedBy,
                OperationDate = row.OperationDate
            }));

            return marks
                .OrderBy(x => x.Timestamp)
                .ThenBy(x => x.Source)
                .ThenBy(x => x.RecordId)
                .ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las marcas para el contexto de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las marcas para el contexto de asistencia.", ex);
        }
    }

    private static bool IsPreviousDayClosureMark(string? checkType)
        => string.Equals(checkType, "L", StringComparison.OrdinalIgnoreCase);

    private static AttendanceMarkType ResolveMarkType(string? checkType)
    {
        if (string.IsNullOrWhiteSpace(checkType))
        {
            return AttendanceMarkType.Unknown;
        }

        return checkType.Trim().ToUpperInvariant() switch
        {
            "I" => AttendanceMarkType.CheckIn,
            "O" => AttendanceMarkType.CheckOut,
            "L" => AttendanceMarkType.PreviousDayClosure,
            _ => AttendanceMarkType.Unknown
        };
    }

    private sealed class CheckInOutRow
    {
        public int PersonId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? CheckType { get; set; }
        public int? VerifyCode { get; set; }
        public string? SensorId { get; set; }
        public string? DeviceSerialNumber { get; set; }
        public string? MemoInfo { get; set; }
        public string? WorkCode { get; set; }
    }

    private sealed class CheckExactRow
    {
        public int RecordId { get; set; }
        public int PersonId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? CheckType { get; set; }
        public bool? IsAdded { get; set; }
        public string? Note { get; set; }
        public bool? IsModified { get; set; }
        public bool? IsDeleted { get; set; }
        public short? InCount { get; set; }
        public bool? IsCounted { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? OperationDate { get; set; }
    }
}