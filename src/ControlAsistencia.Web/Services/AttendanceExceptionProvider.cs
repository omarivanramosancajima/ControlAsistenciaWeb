using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendanceExceptionProvider : IAttendanceExceptionProvider
{
    private readonly string _connectionString;

    public AttendanceExceptionProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<AttendanceException>> GetExceptionsAsync(int personId, DateOnly date)
    {
        const string sql = @"
SELECT
    U.USERID AS PersonId,
    L.LeaveId AS LeaveId,
    L.LeaveName AS LeaveName,
    U.STARTSPECDAY AS StartDateTime,
    U.ENDSPECDAY AS EndDateTime,
    U.[DATE] AS RegisteredAt,
    U.YUANYING AS Reason,
    L.MinUnit AS MinUnit,
    L.Unit AS Unit,
    L.Classify AS Classify,
    L.ReportSymbol AS ReportSymbol,
    L.Deduct AS Deduct,
    L.Color AS Color
FROM dbo.USER_SPEDAY U WITH (NOLOCK)
INNER JOIN dbo.LeaveClass L WITH (NOLOCK) ON L.LeaveId = U.DATEID
WHERE U.USERID = @PersonId
  AND CAST(U.STARTSPECDAY AS date) <= @TargetDate
  AND CAST(ISNULL(U.ENDSPECDAY, U.STARTSPECDAY) AS date) >= @TargetDate
ORDER BY U.STARTSPECDAY ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<AttendanceException>(sql, new
            {
                PersonId = personId,
                TargetDate = date.ToDateTime(TimeOnly.MinValue).Date
            });

            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las excepciones de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las excepciones de asistencia.", ex);
        }
    }
}