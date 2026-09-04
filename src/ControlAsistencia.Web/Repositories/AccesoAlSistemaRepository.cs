using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public interface IAccesoAlSistemaRepository
{
    Task<bool> CanManageAccessAsync(int userId);
    Task<(IReadOnlyList<AccesoAlSistemaItemViewModel> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize, string? search);
    Task<AccesoAlSistemaItemViewModel?> GetByIdAsync(int userId);
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<IReadOnlyList<AccesoAlSistemaEmployeeItemViewModel>> GetEmployeesWithoutAccessByDepartmentAsync(int deptId);
    Task<AccesoAlSistemaEmployeeItemViewModel?> GetEmployeeWithoutAccessAsync(int userId);
    Task<OperationResult> CreateAccessAsync(int userId, short securityFlags, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAccessAsync(int userId, short securityFlags, string operatorName, string machineAlias);
    Task<OperationResult> DeleteAccessAsync(int userId, string operatorName, string machineAlias);
    Task RegisterViewAuditAsync(string operatorName, string machineAlias);
}

public class AccesoAlSistemaRepository : IAccesoAlSistemaRepository
{
    private readonly string _connectionString;

    private static readonly IReadOnlyDictionary<short, string> AccessTypes =
        new Dictionary<short, string>
        {
            [15] = "Administrador",
            [7] = "Supervisor",
            [8] = "Supervisor de Departamento 1",
            [9] = "Supervisor de Departamento 2",
            [5] = "Usuario Autoservicio"
        };

    public AccesoAlSistemaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<bool> CanManageAccessAsync(int userId)
    {
        const string sql = @"
SELECT CAST(CASE WHEN SECURITYFLAGS IN (15, 7) THEN 1 ELSE 0 END AS bit)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<(IReadOnlyList<AccesoAlSistemaItemViewModel> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber, int pageSize, string? search)
    {
        const string sql = @"
SELECT
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
    U.SECURITYFLAGS AS SecurityFlags
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.SECURITYFLAGS IN (15, 7, 8, 9, 5)
  AND
  (
      @Search IS NULL
      OR U.NAME LIKE '%' + @Search + '%'
      OR U.SSN LIKE '%' + @Search + '%'
  )
ORDER BY U.NAME ASC, U.USERID ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.USERINFO U WITH (NOLOCK)
WHERE U.SECURITYFLAGS IN (15, 7, 8, 9, 5)
  AND
  (
      @Search IS NULL
      OR U.NAME LIKE '%' + @Search + '%'
      OR U.SSN LIKE '%' + @Search + '%'
  );";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (pageNumber - 1) * pageSize,
                PageSize = pageSize,
                Search = search
            });

            var items = (await multi.ReadAsync<AccesoAlSistemaItemViewModel>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();

            foreach (var item in items)
            {
                item.AccessDescription = GetAccessDescription(item.SecurityFlags);
                item.PhotoBase64 = item.Photo is { Length: > 0 }
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                    : null;
            }

            return (items, totalRecords);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener los accesos al sistema.", ex);
        }
    }

    public async Task<AccesoAlSistemaItemViewModel?> GetByIdAsync(int userId)
    {
        const string sql = @"
SELECT TOP (1)
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
    U.SECURITYFLAGS AS SecurityFlags
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID = @UserId
  AND U.SECURITYFLAGS IN (15, 7, 8, 9, 5);";

        await using var connection = new SqlConnection(_connectionString);
        var item = await connection.QueryFirstOrDefaultAsync<AccesoAlSistemaItemViewModel>(
            sql, new { UserId = userId });

        if (item is null)
            return null;

        item.AccessDescription = GetAccessDescription(item.SecurityFlags);
        item.PhotoBase64 = item.Photo is { Length: > 0 }
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
            : null;

        return item;
    }

    public async Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync()
    {
        const string sql = @"
WITH DepartmentTree AS
(
    SELECT
        D.DEPTID AS DeptId,
        ISNULL(D.DEPTNAME, '') AS DeptName,
        D.SUPDEPTID AS SupDeptId,
        0 AS [Level],
        CAST('>' + CAST(D.DEPTID AS varchar(20)) + '>' AS varchar(max)) AS PathIds,
        CAST(ISNULL(D.DEPTNAME, '') AS varchar(max)) AS HierarchyName
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    WHERE D.SUPDEPTID = 0

    UNION ALL

    SELECT
        C.DEPTID,
        ISNULL(C.DEPTNAME, ''),
        C.SUPDEPTID,
        P.[Level] + 1,
        CAST(P.PathIds + CAST(C.DEPTID AS varchar(20)) + '>' AS varchar(max)),
        CAST(P.HierarchyName + ' > ' + ISNULL(C.DEPTNAME, '') AS varchar(max))
    FROM DepartmentTree P
    INNER JOIN dbo.DEPARTMENTS C WITH (NOLOCK)
        ON C.SUPDEPTID = P.DEPTID
    WHERE CHARINDEX('>' + CAST(C.DEPTID AS varchar(20)) + '>', P.PathIds) = 0
)
SELECT DeptId, DeptName, SupDeptId, [Level], HierarchyName
FROM DepartmentTree
ORDER BY HierarchyName
OPTION (MAXRECURSION 0);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var result = await connection.QueryAsync<DepartmentDTO>(sql);
            return result.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las áreas.", ex);
        }
    }

    public async Task<IReadOnlyList<AccesoAlSistemaEmployeeItemViewModel>> GetEmployeesWithoutAccessByDepartmentAsync(int deptId)
    {
        const string sql = @"
SELECT
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.DEFAULTDEPTID = @DeptId
  AND (U.SECURITYFLAGS IS NULL OR U.SECURITYFLAGS = 0)
ORDER BY U.NAME ASC, U.USERID ASC;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            var result = (await connection.QueryAsync<AccesoAlSistemaEmployeeItemViewModel>(
                sql, new { DeptId = deptId })).ToList();

            foreach (var item in result)
            {
                item.PhotoBase64 = item.Photo is { Length: > 0 }
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                    : null;
            }

            return result;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las personas sin acceso.", ex);
        }
    }

    public async Task<AccesoAlSistemaEmployeeItemViewModel?> GetEmployeeWithoutAccessAsync(int userId)
    {
        const string sql = @"
SELECT TOP (1)
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID = @UserId
  AND (U.SECURITYFLAGS IS NULL OR U.SECURITYFLAGS = 0);";

        await using var connection = new SqlConnection(_connectionString);
        var item = await connection.QueryFirstOrDefaultAsync<AccesoAlSistemaEmployeeItemViewModel>(
            sql, new { UserId = userId });

        if (item is not null && item.Photo is { Length: > 0 })
            item.PhotoBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}";

        return item;
    }

    public async Task<OperationResult> CreateAccessAsync(
        int userId, short securityFlags, string operatorName, string machineAlias)
    {
        if (!AccessTypes.ContainsKey(securityFlags))
            return OperationResult.Fail("El tipo de acceso seleccionado no es válido.");

        const string sql = @"
DECLARE @Name VARCHAR(24);
DECLARE @Ssn VARCHAR(20);
DECLARE @CurrentFlags SMALLINT;

SELECT
    @Name = NAME,
    @Ssn = SSN,
    @CurrentFlags = SECURITYFLAGS
FROM dbo.USERINFO
WHERE USERID = @UserId;

IF @Name IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.USERINFO WHERE USERID = @UserId)
    THROW 52001, 'La persona seleccionada no existe.', 1;

IF @CurrentFlags IS NOT NULL AND @CurrentFlags <> 0
    THROW 52002, 'La persona seleccionada ya tiene acceso al sistema.', 1;

IF NULLIF(LTRIM(RTRIM(@Ssn)), '') IS NULL
    THROW 52003, 'La persona seleccionada no tiene DNI. No es posible inicializar la contraseña.', 1;

UPDATE dbo.USERINFO
SET SECURITYFLAGS = @SecurityFlags,
    [PASSWORD] = CASE
                    WHEN [PASSWORD] IS NULL THEN @Ssn
                    ELSE [PASSWORD]
                END
WHERE USERID = @UserId;

IF @@ROWCOUNT <> 1
    THROW 52004, 'No fue posible registrar el acceso.', 1;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES
(
    LEFT(@Operator, 20),
    GETDATE(),
    LEFT(@MachineAlias, 20),
    0,
    LEFT('Crea Acceso: ' + ISNULL(@Name, '') + ' (' + CAST(@SecurityFlags AS VARCHAR(3)) + ')', 50)
);";

        return await ExecuteOperationAsync(sql, new
        {
            UserId = userId,
            SecurityFlags = securityFlags,
            Operator = operatorName,
            MachineAlias = machineAlias
        }, "No fue posible crear el acceso al sistema.");
    }

    public async Task<OperationResult> UpdateAccessAsync(
        int userId, short securityFlags, string operatorName, string machineAlias)
    {
        if (!AccessTypes.ContainsKey(securityFlags))
            return OperationResult.Fail("El tipo de acceso seleccionado no es válido.");

        const string sql = @"
DECLARE @Name VARCHAR(24);
DECLARE @CurrentFlags SMALLINT;

SELECT
    @Name = NAME,
    @CurrentFlags = SECURITYFLAGS
FROM dbo.USERINFO
WHERE USERID = @UserId;

IF @Name IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.USERINFO WHERE USERID = @UserId)
    THROW 52005, 'La persona seleccionada no existe.', 1;

IF @CurrentFlags IS NULL OR @CurrentFlags NOT IN (15, 7, 8, 9, 5)
    THROW 52006, 'La persona seleccionada ya no tiene un acceso válido para editar.', 1;

IF @CurrentFlags = @SecurityFlags
    THROW 52007, 'El nivel de acceso no presenta cambios.', 1;

UPDATE dbo.USERINFO
SET SECURITYFLAGS = @SecurityFlags
WHERE USERID = @UserId;

IF @@ROWCOUNT <> 1
    THROW 52008, 'No fue posible actualizar el acceso.', 1;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES
(
    LEFT(@Operator, 20),
    GETDATE(),
    LEFT(@MachineAlias, 20),
    0,
    LEFT('Edita Acceso: ' + ISNULL(@Name, '') + ' (' + CAST(@CurrentFlags AS VARCHAR(3)) + '->' + CAST(@SecurityFlags AS VARCHAR(3)) + ')', 50)
);";

        return await ExecuteOperationAsync(sql, new
        {
            UserId = userId,
            SecurityFlags = securityFlags,
            Operator = operatorName,
            MachineAlias = machineAlias
        }, "No fue posible actualizar el acceso al sistema.");
    }

    public async Task<OperationResult> DeleteAccessAsync(
        int userId, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @Name VARCHAR(24);
DECLARE @CurrentFlags SMALLINT;

SELECT
    @Name = NAME,
    @CurrentFlags = SECURITYFLAGS
FROM dbo.USERINFO
WHERE USERID = @UserId;

IF @Name IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.USERINFO WHERE USERID = @UserId)
    THROW 52009, 'La persona seleccionada no existe.', 1;

IF @CurrentFlags IS NULL OR @CurrentFlags NOT IN (15, 7, 8, 9, 5)
    THROW 52010, 'La persona seleccionada no tiene un acceso válido para eliminar.', 1;

UPDATE dbo.USERINFO
SET SECURITYFLAGS = NULL,
    [PASSWORD] = NULL
WHERE USERID = @UserId;

IF @@ROWCOUNT <> 1
    THROW 52011, 'No fue posible eliminar el acceso.', 1;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES
(
    LEFT(@Operator, 20),
    GETDATE(),
    LEFT(@MachineAlias, 20),
    0,
    LEFT('Elimina Acceso: ' + ISNULL(@Name, '') + ' (' + CAST(@CurrentFlags AS VARCHAR(3)) + ')', 50)
);";

        return await ExecuteOperationAsync(sql, new
        {
            UserId = userId,
            Operator = operatorName,
            MachineAlias = machineAlias
        }, "No fue posible eliminar el acceso al sistema.");
    }

    public async Task RegisterViewAuditAsync(string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES
(
    LEFT(@Operator, 20),
    GETDATE(),
    LEFT(@MachineAlias, 20),
    0,
    'Visualiza Acceso al Sistema'
);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            Operator = operatorName,
            MachineAlias = machineAlias
        });
    }

    private async Task<OperationResult> ExecuteOperationAsync(
        string sql, object parameters, string genericError)
    {
        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, parameters, transaction);
            await transaction.CommitAsync();

            return OperationResult.Ok("Operación realizada correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail(ex.Message);
        }
        catch (Exception)
        {
            return OperationResult.Fail(genericError);
        }
    }

    private static string GetAccessDescription(short? securityFlags) =>
        securityFlags.HasValue && AccessTypes.TryGetValue(securityFlags.Value, out var description)
            ? description
            : "Sin acceso";
}
