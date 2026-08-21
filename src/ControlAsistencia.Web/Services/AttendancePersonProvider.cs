using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendancePersonProvider : IAttendancePersonProvider
{
    private readonly string _connectionString;

    public AttendancePersonProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<AttendancePersonInfo?> GetByPersonIdAsync(int personId)
    {
        const string sql = @"
SELECT TOP (1)
    U.USERID AS PersonId,
    U.BADGENUMBER AS PersonCode,
    U.SSN AS PersonDocumentNumber,
    U.NAME AS PersonName,
    U.DEFAULTDEPTID AS DepartmentId,
    D.DEPTNAME AS DepartmentName
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID = @PersonId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AttendancePersonInfo>(sql, new { PersonId = personId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener la persona para el contexto de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener la persona para el contexto de asistencia.", ex);
        }
    }
}