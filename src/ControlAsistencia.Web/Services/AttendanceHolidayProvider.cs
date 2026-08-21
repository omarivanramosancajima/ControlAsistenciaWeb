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
        const string sql = @"
SELECT TOP (1)
    H.HOLIDAYNAME AS HolidayName
FROM dbo.HOLIDAYS H WITH (NOLOCK)
WHERE
    (H.HOLIDAYYEAR IS NULL OR H.HOLIDAYYEAR = @Year)
    AND (H.HOLIDAYMONTH IS NULL OR H.HOLIDAYMONTH = @Month)
    AND H.HOLIDAYDAY = @Day
ORDER BY H.HOLIDAYYEAR DESC, H.HOLIDAYMONTH DESC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var holidayName = await connection.QueryFirstOrDefaultAsync<string?>(sql, new
            {
                Year = date.Year,
                Month = date.Month,
                Day = date.Day
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