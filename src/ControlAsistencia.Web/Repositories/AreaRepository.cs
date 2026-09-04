using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class AreaRepository : IAreaRepository
{
    private readonly string _connectionString;

    public AreaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<AreaItemViewModel>> GetHierarchyAsync()
    {
        const string sql = @"
WITH DepartmentTree AS
(
    SELECT
        D.DEPTID,
        ISNULL(D.DEPTNAME, '') AS DEPTNAME,
        D.SUPDEPTID,
        0 AS [Level],
        CAST(
            CASE
                WHEN D.SUPDEPTID = 0
                     AND D.InheritParentSch IS NULL
                     AND D.InheritDeptSch IS NULL
                THEN 1 ELSE 0
            END AS bit) AS IsRoot,
        CAST(ISNULL(D.DEPTNAME, '') AS VARCHAR(1000)) AS SortPath
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    WHERE D.SUPDEPTID = 0

    UNION ALL

    SELECT
        C.DEPTID,
        ISNULL(C.DEPTNAME, '') AS DEPTNAME,
        C.SUPDEPTID,
        P.[Level] + 1,
        CAST(0 AS bit) AS IsRoot,
        CAST(P.SortPath + ' > ' + ISNULL(C.DEPTNAME, '') AS VARCHAR(1000))
    FROM dbo.DEPARTMENTS C WITH (NOLOCK)
    INNER JOIN DepartmentTree P
        ON P.DEPTID = C.SUPDEPTID
)
SELECT
    T.DEPTID AS DeptId,
    T.DEPTNAME AS DeptName,
    T.SUPDEPTID AS SupDeptId,
    T.[Level],
    T.IsRoot,
    CAST(CASE WHEN EXISTS
        (SELECT 1 FROM dbo.DEPARTMENTS CH WITH (NOLOCK)
         WHERE CH.SUPDEPTID = T.DEPTID)
        THEN 1 ELSE 0 END AS bit) AS HasChildren,
    (SELECT COUNT(1)
     FROM dbo.USERINFO U WITH (NOLOCK)
     WHERE U.DEFAULTDEPTID = T.DEPTID) AS EmployeeCount
FROM DepartmentTree T
ORDER BY T.SortPath
OPTION (MAXRECURSION 0);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var result = await connection.QueryAsync<AreaItemViewModel>(sql);
            return result.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las áreas.", ex);
        }
    }

    public async Task<AreaItemViewModel?> GetByIdAsync(int deptId)
    {
        const string sql = @"
SELECT
    D.DEPTID AS DeptId,
    ISNULL(D.DEPTNAME, '') AS DeptName,
    D.SUPDEPTID AS SupDeptId,
    CAST(CASE WHEN D.SUPDEPTID = 0
                   AND D.InheritParentSch IS NULL
                   AND D.InheritDeptSch IS NULL
              THEN 1 ELSE 0 END AS bit) AS IsRoot,
    CAST(CASE WHEN EXISTS
        (SELECT 1 FROM dbo.DEPARTMENTS C WITH (NOLOCK)
         WHERE C.SUPDEPTID = D.DEPTID)
        THEN 1 ELSE 0 END AS bit) AS HasChildren,
    (SELECT COUNT(1) FROM dbo.USERINFO U WITH (NOLOCK)
     WHERE U.DEFAULTDEPTID = D.DEPTID) AS EmployeeCount
FROM dbo.DEPARTMENTS D WITH (NOLOCK)
WHERE D.DEPTID = @DeptId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            return await connection.QuerySingleOrDefaultAsync<AreaItemViewModel>(
                sql, new { DeptId = deptId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al consultar el área.", ex);
        }
    }

    public async Task<AreaOperationResult> CreateAsync(int parentDeptId, string deptName)
    {
        var name = (deptName ?? string.Empty).Trim();
        if (name.Length == 0)
            return AreaOperationResult.Fail("Debe ingresar el nombre del área.");
        if (name.Length > 30)
            return AreaOperationResult.Fail("El nombre del área no puede superar los 30 caracteres.");

        const string sql = @"
DECLARE @ParentExists bit = 0;
DECLARE @ParentIsRoot bit = 0;

SELECT
    @ParentExists = 1,
    @ParentIsRoot =
        CASE WHEN SUPDEPTID = 0
                   AND InheritParentSch IS NULL
                   AND InheritDeptSch IS NULL
              THEN 1 ELSE 0 END
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @ParentDeptId;

IF @ParentExists = 0
    THROW 51001, 'El área padre seleccionada no existe.', 1;

INSERT INTO dbo.DEPARTMENTS
(
    DEPTNAME, SUPDEPTID,
    InheritParentSch, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, InLate, OutEarly, InheritDeptRule,
    MinAutoSchInterval, RegisterOT, DefaultSchId,
    ATT, Holiday, OverTime
)
VALUES
(
    @DeptName, @ParentDeptId,
    1, 1, 1,
    1, 1, 1, 1,
    24, 1, 1,
    1, 1, 1
);

SELECT CAST(SCOPE_IDENTITY() AS int);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var id = await connection.ExecuteScalarAsync<int>(
                sql, new { ParentDeptId = parentDeptId, DeptName = name });
            return AreaOperationResult.Ok($"Área creada correctamente. ID: {id}.");
        }
        catch (SqlException ex)
        {
            return AreaOperationResult.Fail($"Error SQL al crear el área: {ex.Message}");
        }
    }

    public async Task<AreaOperationResult> UpdateAsync(int deptId, string deptName)
    {
        var name = (deptName ?? string.Empty).Trim();
        if (name.Length == 0)
            return AreaOperationResult.Fail("Debe ingresar el nombre del área.");
        if (name.Length > 30)
            return AreaOperationResult.Fail("El nombre del área no puede superar los 30 caracteres.");

        const string sql = @"
DECLARE @IsRoot bit = 0;

SELECT @IsRoot =
    CASE WHEN SUPDEPTID = 0
               AND InheritParentSch IS NULL
               AND InheritDeptSch IS NULL
          THEN 1 ELSE 0 END
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @DeptId;

IF @@ROWCOUNT = 0
    THROW 51002, 'El área no existe.', 1;

IF @IsRoot = 1
    THROW 51003, 'Los nodos raíz no pueden editar su nombre.', 1;

UPDATE dbo.DEPARTMENTS
SET DEPTNAME = @DeptName
WHERE DEPTID = @DeptId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var affected = await connection.ExecuteAsync(sql, new { DeptId = deptId, DeptName = name });
            return affected == 1
                ? AreaOperationResult.Ok("Área actualizada correctamente.")
                : AreaOperationResult.Fail("No se actualizó el área.");
        }
        catch (SqlException ex)
        {
            return AreaOperationResult.Fail($"Error SQL al actualizar el área: {ex.Message}");
        }
    }

    public async Task<AreaOperationResult> DeleteAsync(int deptId)
    {
        const string sql = @"
DECLARE @IsRoot bit;
DECLARE @HasChildren bit;
DECLARE @EmployeeCount int;

SELECT
    @IsRoot =
        CASE WHEN SUPDEPTID = 0
                   AND InheritParentSch IS NULL
                   AND InheritDeptSch IS NULL
              THEN 1 ELSE 0 END
FROM dbo.DEPARTMENTS WITH (NOLOCK)
WHERE DEPTID = @DeptId;

IF @IsRoot IS NULL
    THROW 51004, 'El área seleccionada no existe.', 1;

IF @IsRoot = 1
    THROW 51005, 'Los nodos raíz no pueden eliminarse.', 1;

SELECT @HasChildren =
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.DEPARTMENTS WITH (NOLOCK)
        WHERE SUPDEPTID = @DeptId
    ) THEN 1 ELSE 0 END;

IF @HasChildren = 1
    THROW 51006, 'El área no puede eliminarse porque tiene áreas hijas.', 1;

SELECT @EmployeeCount = COUNT(1)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE DEFAULTDEPTID = @DeptId;

IF @EmployeeCount > 0
    THROW 51007, 'El área no puede eliminarse porque tiene empleados asignados.', 1;

DELETE FROM dbo.DEPARTMENTS
WHERE DEPTID = @DeptId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var affected = await connection.ExecuteAsync(sql, new { DeptId = deptId });
            return affected == 1
                ? AreaOperationResult.Ok("Área eliminada correctamente.")
                : AreaOperationResult.Fail("No se eliminó el área.");
        }
        catch (SqlException ex)
        {
            return AreaOperationResult.Fail($"Error SQL al eliminar el área: {ex.Message}");
        }
    }
}
