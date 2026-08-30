using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendanceHolidayProvider : IAttendanceHolidayProvider
{
    private readonly string _connectionString;

    public AttendanceHolidayProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<AttendanceHolidayInfo> GetHolidayAsync(DateOnly date)
    {
        // [ASISTWEB][SEC.01.01]
        const string sql = @"
SELECT TOP (1)
    H.HOLIDAYNAME AS HolidayName
FROM dbo.HOLIDAYS H WITH (NOLOCK)
WHERE CAST(H.STARTTIME AS date) = @TargetDate
ORDER BY H.STARTTIME ASC, H.HOLIDAYID ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var holidayName = await connection.QueryFirstOrDefaultAsync<string?>(sql, new
            {
                TargetDate = date.ToDateTime(TimeOnly.MinValue).Date
            });

            return new AttendanceHolidayInfo
            {
                IsHoliday = !string.IsNullOrWhiteSpace(holidayName),
                HolidayName = holidayName
            };
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener el feriado de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener el feriado de asistencia.", ex);
        }
    }
}