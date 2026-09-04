using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class ReubicacionEmpleadoRepository : IReubicacionEmpleadoRepository
{
    private readonly string _connectionString;

    public ReubicacionEmpleadoRepository(IConfiguration configuration)
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
SELECT DeptId, DeptName, SupDeptId, [Level], HierarchyName
FROM DepartmentTree
ORDER BY HierarchyName
OPTION (MAXRECURSION 100);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var items = await connection.QueryAsync<DepartmentDTO>(sql);
            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener las áreas.", ex);
        }
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @DeptId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            return await connection.ExecuteScalarAsync<int>(sql, new { DeptId = deptId }) > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al validar el área seleccionada.", ex);
        }
    }

    public async Task<IReadOnlyList<ReubicacionEmpleadoEmployeeItemViewModel>>
        GetEmployeesByDepartmentAsync(int deptId, bool includeSubDependencies)
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
    INNER JOIN DepartmentScope DS ON DS.DEPTID = D.SUPDEPTID
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
    (@IncludeSubDependencies = 1 AND U.DEFAULTDEPTID IN
        (SELECT DEPTID FROM DepartmentScope))
    OR
    (@IncludeSubDependencies = 0 AND U.DEFAULTDEPTID = @DeptId)
ORDER BY U.BADGENUMBER ASC
OPTION (MAXRECURSION 100);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var items = (await connection.QueryAsync<ReubicacionEmpleadoEmployeeItemViewModel>(
                sql, new { DeptId = deptId, IncludeSubDependencies = includeSubDependencies }))
                .ToList();

            foreach (var item in items)
            {
                item.PrivilegeDescription = PrivilegeHelper.GetDescription(item.Privilege);
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
    }

    public async Task<IReadOnlyList<ReubicacionEmpleadoEmployeeItemViewModel>>
        GetEmployeesByUserIdsAsync(IReadOnlyCollection<int> userIds)
    {
        if (userIds.Count == 0)
            return [];

        const string sql = @"
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
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID IN @UserIds
ORDER BY U.BADGENUMBER ASC;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var items = (await connection.QueryAsync<ReubicacionEmpleadoEmployeeItemViewModel>(
                sql, new { UserIds = userIds })).ToList();

            foreach (var item in items)
            {
                item.PrivilegeDescription = PrivilegeHelper.GetDescription(item.Privilege);
                item.PhotoBase64 = item.Photo is { Length: > 0 }
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                    : null;
            }

            return items;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener las personas seleccionadas.", ex);
        }
    }

    public async Task<ReubicacionEmpleadoProgressItemViewModel> TransferEmployeeAsync(
        int userId, int targetDeptId)
    {
        if (targetDeptId <= 0 || targetDeptId > short.MaxValue)
        {
            return new ReubicacionEmpleadoProgressItemViewModel
            {
                UserId = userId,
                Status = "Error",
                Result = "El área seleccionada no es válida para USERINFO.DEFAULTDEPTID.",
                Success = false
            };
        }

        const string selectSql = @"
SELECT USERID, BADGENUMBER, ISNULL(NAME, '') AS Name,
       DEFAULTDEPTID AS DefaultDeptId
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";

        const string deptSql = @"
SELECT DEPTID, DEPTNAME
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @DeptId;";

        const string updateSql = @"
UPDATE dbo.USERINFO
SET DEFAULTDEPTID = @TargetDeptId
WHERE USERID = @UserId
  AND (DEFAULTDEPTID IS NULL OR DEFAULTDEPTID <> @TargetDeptId);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();

            var employee = await connection.QuerySingleOrDefaultAsync<dynamic>(
                selectSql, new { UserId = userId });

            if (employee is null)
            {
                return new ReubicacionEmpleadoProgressItemViewModel
                {
                    UserId = userId,
                    Status = "Error",
                    Result = "La persona no existe en USERINFO.",
                    Success = false
                };
            }

            var department = await connection.QuerySingleOrDefaultAsync<dynamic>(
                deptSql, new { DeptId = targetDeptId });

            if (department is null)
            {
                return new ReubicacionEmpleadoProgressItemViewModel
                {
                    UserId = userId,
                    BadgeNumber = employee.BADGENUMBER ?? string.Empty,
                    Name = employee.Name ?? string.Empty,
                    Status = "Error",
                    Result = "El área seleccionada no existe.",
                    Success = false
                };
            }

            var result = new ReubicacionEmpleadoProgressItemViewModel
            {
                UserId = userId,
                BadgeNumber = employee.BADGENUMBER ?? string.Empty,
                Name = employee.Name ?? string.Empty
            };

            int? currentDeptId = employee.DefaultDeptId is null
                ? null
                : Convert.ToInt32(employee.DefaultDeptId);

            if (currentDeptId == targetDeptId)
            {
                result.Status = "Sin cambio";
                result.Result = "La persona ya pertenece al área seleccionada.";
                result.Success = true;
                return result;
            }

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await connection.ExecuteAsync(
                    updateSql,
                    new { UserId = userId, TargetDeptId = (short)targetDeptId },
                    transaction);

                await transaction.CommitAsync();

                result.Status = "Completado";
                result.Result = $"Trasladado a {department.DEPTNAME ?? string.Empty}.";
                result.Success = true;
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (SqlException ex)
        {
            return new ReubicacionEmpleadoProgressItemViewModel
            {
                UserId = userId,
                Status = "Error",
                Result = $"Error SQL: {ex.Message}",
                Success = false
            };
        }
        catch (Exception ex)
        {
            return new ReubicacionEmpleadoProgressItemViewModel
            {
                UserId = userId,
                Status = "Error",
                Result = ex.Message,
                Success = false
            };
        }
    }
}
