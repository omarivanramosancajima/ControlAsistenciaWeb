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
        // [ASISTWEB][SEC.00]
        const string sql = @"
WITH DepartmentChain AS
(
    SELECT
        D.DEPTID,
        D.DEPTNAME,
        D.SUPDEPTID,
        CAST(D.DEPTID AS varchar(4000)) AS PathIds,
        0 AS Depth
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    INNER JOIN dbo.USERINFO U WITH (NOLOCK) ON U.DEFAULTDEPTID = D.DEPTID
    WHERE U.USERID = @PersonId

    UNION ALL

    SELECT
        P.DEPTID,
        P.DEPTNAME,
        P.SUPDEPTID,
        CAST(DC.PathIds + '>' + CAST(P.DEPTID AS varchar(50)) AS varchar(4000)) AS PathIds,
        DC.Depth + 1 AS Depth
    FROM DepartmentChain DC
    INNER JOIN dbo.DEPARTMENTS P WITH (NOLOCK) ON P.DEPTID = DC.SUPDEPTID
    WHERE DC.SUPDEPTID > 0
      AND CHARINDEX('>' + CAST(P.DEPTID AS varchar(50)) + '>', '>' + DC.PathIds + '>') = 0
),
ResolvedCompany AS
(
    SELECT TOP (1)
        C.SCIA_TAXID AS CompanyTaxId,
        C.SCIA_DESCRIP AS CompanyName,
        C.DEPTID AS CompanyDepartmentId,
        DC.Depth
    FROM DepartmentChain DC
    INNER JOIN dbo.COMPANY C WITH (NOLOCK) ON C.DEPTID = DC.DEPTID
    ORDER BY DC.Depth ASC
)
SELECT TOP (1)
    U.USERID AS PersonId,
    U.BADGENUMBER AS PersonCode,
    U.SSN AS PersonDocumentNumber,
    U.NAME AS PersonName,
    U.DEFAULTDEPTID AS DepartmentId,
    D.DEPTNAME AS DepartmentName,
    ISNULL(RC.CompanyTaxId, '') AS CompanyTaxId,
    ISNULL(RC.CompanyName, '') AS CompanyName,
    RC.CompanyDepartmentId,
    CASE
        WHEN U.DEFAULTDEPTID IS NULL THEN 'DEFAULTDEPTID_NULL'
        WHEN D.DEPTID IS NULL THEN 'DEPARTMENT_NOT_FOUND'
        WHEN RC.CompanyDepartmentId IS NULL THEN 'COMPANY_NOT_RESOLVED_FROM_DEPARTMENT_CHAIN'
        ELSE 'COMPANY_RESOLVED_FROM_DEPARTMENT_CHAIN'
    END AS CompanyResolutionDiagnostic
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
OUTER APPLY (SELECT TOP (1) * FROM ResolvedCompany) RC
WHERE U.USERID = @PersonId
OPTION (MAXRECURSION 32);";

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