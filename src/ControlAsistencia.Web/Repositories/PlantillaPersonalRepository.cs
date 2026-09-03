using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class PlantillaPersonalRepository : IPlantillaPersonalRepository
{
    private readonly string _connectionString;

    public PlantillaPersonalRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync()
    {
        const string sql = @"
WITH DepartmentTree AS
(
    SELECT
        DEPTID AS DeptId,
        DEPTNAME AS DeptName,
        SUPDEPTID AS SupDeptId,
        0 AS [Level],
        CAST(ISNULL(DEPTNAME, '') AS VARCHAR(500)) AS HierarchyName
    FROM dbo.DEPARTMENTS WITH (NOLOCK)
    WHERE SUPDEPTID = 0

    UNION ALL

    SELECT
        D.DEPTID,
        D.DEPTNAME,
        D.SUPDEPTID,
        T.Level + 1,
        CAST(T.HierarchyName + ' > ' + ISNULL(D.DEPTNAME, '') AS VARCHAR(500))
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    INNER JOIN DepartmentTree T
        ON T.DeptId = D.SUPDEPTID
)
SELECT
    DeptId,
    DeptName,
    SupDeptId,
    [Level],
    HierarchyName
FROM DepartmentTree
ORDER BY HierarchyName
OPTION (MAXRECURSION 100);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<DepartmentDTO>(sql);
            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener las áreas.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error inesperado al obtener las áreas.", ex);
        }
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @DeptId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                sql,
                new { DeptId = deptId }) > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al validar el área seleccionada.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error inesperado al validar el área seleccionada.", ex);
        }
    }

    public async Task<IReadOnlyList<PlantillaPersonalEmployeeItemViewModel>>
        GetEmployeesByDepartmentAsync(
            int deptId,
            bool includeSubDependencies)
    {
        const string sql = @"
WITH DepartmentScope AS
(
    SELECT DEPTID
    FROM dbo.DEPARTMENTS WITH (NOLOCK)
    WHERE DEPTID = @DeptId

    UNION ALL

    SELECT D.DEPTID
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    INNER JOIN DepartmentScope DS
        ON DS.DEPTID = D.SUPDEPTID
)
SELECT
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
    U.privilege AS Privilege
FROM dbo.USERINFO U WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE
    (@IncludeSubDependencies = 1
        AND U.DEFAULTDEPTID IN
        (
            SELECT DEPTID
            FROM DepartmentScope
        ))
    OR
    (@IncludeSubDependencies = 0
        AND U.DEFAULTDEPTID = @DeptId)
ORDER BY U.BADGENUMBER ASC
OPTION (MAXRECURSION 100);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);

            var items = (await connection.QueryAsync<PlantillaPersonalEmployeeItemViewModel>(
                sql,
                new
                {
                    DeptId = deptId,
                    IncludeSubDependencies = includeSubDependencies
                })).ToList();

            foreach (var item in items)
            {
                item.PrivilegeDescription =
                    PrivilegeHelper.GetDescription(item.Privilege);

                item.PhotoBase64 = item.Photo is { Length: > 0 }
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                    : null;
            }

            return items;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener las personas del área.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error inesperado al obtener las personas del área.", ex);
        }
    }
}
