using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class AttendanceReportRepository : IAttendanceReportRepository
{
    private readonly string _connectionString;

    public AttendanceReportRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<AttendanceReportFilterPerson>> GetFilterPersonsAsync(string? personName, string? areaName)
    {
        const string sql = @"
SELECT
    U.USERID AS PersonId,
    U.BADGENUMBER AS PersonCode,
    U.SSN AS PersonDocumentNumber,
    U.NAME AS PersonName,
    U.DEFAULTDEPTID AS DepartmentId,
    D.DEPTNAME AS DepartmentName
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE (@PersonName IS NULL OR LTRIM(RTRIM(U.NAME)) = @PersonName)
  AND (@AreaName IS NULL OR LTRIM(RTRIM(D.DEPTNAME)) = @AreaName)
ORDER BY U.NAME ASC, U.BADGENUMBER ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<AttendanceReportFilterPerson>(sql, new
            {
                PersonName = NormalizeFilter(personName),
                AreaName = NormalizeFilter(areaName)
            });

            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las personas del reporte de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las personas del reporte de asistencia.", ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableAreasAsync()
    {
        const string sql = @"
SELECT DISTINCT D.DEPTNAME
FROM dbo.DEPARTMENTS D WITH (NOLOCK)
WHERE D.DEPTNAME IS NOT NULL
ORDER BY D.DEPTNAME ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<string>(sql);
            return items.Where(static x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las áreas del reporte de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las áreas del reporte de asistencia.", ex);
        }
    }

    public async Task<AttendanceReportCompanyInfo?> GetCompanyInfoAsync()
    {
        const string sql = @"
SELECT TOP (1)
    C.SCIA_TAXID AS TaxId,
    C.SCIA_DESCRIP AS CompanyName,
    C.DEPTID AS DepartmentId,
    D.DEPTNAME AS DepartmentName
FROM dbo.COMPANY C WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = C.DEPTID
WHERE D.SUPDEPTID = 0
  AND D.InheritParentSch IS NULL
ORDER BY C.COMPANYID ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<AttendanceReportCompanyInfo>(sql);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener la empresa del reporte de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener la empresa del reporte de asistencia.", ex);
        }
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}