using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class JustificarEmpleadoRepository : IJustificarEmpleadoRepository
{
    private readonly string _connectionString;

    public JustificarEmpleadoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
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
    INNER JOIN DepartmentTree T ON T.DeptId = D.SUPDEPTID
)
SELECT DeptId, DeptName, SupDeptId, [Level], HierarchyName
FROM DepartmentTree
ORDER BY HierarchyName
OPTION (MAXRECURSION 100);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<DepartmentDTO>(sql)).ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las áreas.", ex);
        }
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.DEPARTMENTS WITH (NOLOCK) WHERE DEPTID = @DeptId;";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { DeptId = deptId }) > 0;
    }

    public async Task<IReadOnlyList<JustificarEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(
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
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE
    (@IncludeSubDependencies = 1 AND U.DEFAULTDEPTID IN (SELECT DEPTID FROM DepartmentScope))
    OR (@IncludeSubDependencies = 0 AND U.DEFAULTDEPTID = @DeptId)
ORDER BY U.BADGENUMBER ASC
OPTION (MAXRECURSION 100);";

        await using var connection = new SqlConnection(_connectionString);
        var items = (await connection.QueryAsync<JustificarEmpleadoEmployeeItemViewModel>(
            sql, new { DeptId = deptId, IncludeSubDependencies = includeSubDependencies })).ToList();

        foreach (var item in items)
        {
            item.PrivilegeDescription = PrivilegeHelper.GetDescription(item.Privilege);
            item.PhotoBase64 = item.Photo is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                : null;
        }

        return items;
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.USERINFO WITH (NOLOCK) WHERE USERID = @UserId;";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId }) > 0;
    }

    public async Task<JustificarEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId)
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
    U.privilege AS Privilege
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID = @UserId;";

        await using var connection = new SqlConnection(_connectionString);
        var item = await connection.QueryFirstOrDefaultAsync<JustificarEmpleadoEmployeeItemViewModel>(
            sql, new { UserId = userId });

        if (item is null) return null;

        item.PrivilegeDescription = PrivilegeHelper.GetDescription(item.Privilege);
        item.PhotoBase64 = item.Photo is { Length: > 0 }
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
            : null;
        return item;
    }

    public async Task<IReadOnlyList<JustificarEmpleadoExcepcionDisponibleViewModel>> GetExcepcionesAsync()
    {
        const string sql = @"
SELECT
    LeaveId,
    LeaveName,
    Unit,
    Classify
FROM dbo.LeaveClass WITH (NOLOCK)
ORDER BY LeaveName ASC;";

        await using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<JustificarEmpleadoExcepcionDisponibleViewModel>(sql)).ToList();
    }

    public async Task<bool> ExcepcionExistsAsync(int leaveId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.LeaveClass WITH (NOLOCK) WHERE LeaveId = @LeaveId;";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { LeaveId = leaveId }) > 0;
    }

    public async Task<JustificarEmpleadoExcepcionDisponibleViewModel?> GetExcepcionByIdAsync(int leaveId)
    {
        const string sql = @"
SELECT TOP (1)
    LeaveId,
    LeaveName,
    Unit,
    Classify
FROM dbo.LeaveClass WITH (NOLOCK)
WHERE LeaveId = @LeaveId;";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<JustificarEmpleadoExcepcionDisponibleViewModel>(
            sql, new { LeaveId = leaveId });
    }

    public async Task<IReadOnlyList<JustificarEmpleadoExcepcionAsignacionViewModel>> GetAsignacionesByUserAsync(int userId)
    {
        const string sql = @"
SELECT
    U.USERID AS UserId,
    L.LeaveId AS LeaveId,
    L.LeaveName AS LeaveName,
    U.STARTSPECDAY AS StartDateTime,
    U.ENDSPECDAY AS EndDateTime,
    U.YUANYING AS Reason,
    L.Unit AS Unit,
    L.Classify AS Classify,
    L.ReportSymbol AS ReportSymbol,
    U.[DATE] AS RegisteredAt
FROM dbo.USER_SPEDAY U WITH (NOLOCK)
INNER JOIN dbo.LeaveClass L WITH (NOLOCK)
    ON L.LeaveId = U.DATEID
WHERE U.USERID = @UserId
ORDER BY U.ENDSPECDAY DESC, U.STARTSPECDAY DESC;";

        await using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<JustificarEmpleadoExcepcionAsignacionViewModel>(
            sql, new { UserId = userId })).ToList();
    }

    public async Task<JustificarEmpleadoAssignResult> AssignExcepcionAsync(
        JustificarEmpleadoAssignRequest request,
        string operatorName,
        string machineAlias)
    {
        const string usersSql = @"
SELECT USERID AS UserId, BADGENUMBER AS BadgeNumber, NAME AS Name
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID IN @UserIds
ORDER BY BADGENUMBER ASC;";

        const string duplicateSql = @"
IF EXISTS
(
    SELECT 1
    FROM dbo.USER_SPEDAY
    WHERE USERID = @UserId
      AND STARTSPECDAY = @StartDateTime
      AND DATEID = @LeaveId
)
    THROW 50001, 'Ya existe una asignación de la misma excepción para una de las fechas solicitadas.', 1;";

        const string insertSql = @"
INSERT INTO dbo.USER_SPEDAY
(
    USERID,
    STARTSPECDAY,
    ENDSPECDAY,
    DATEID,
    YUANYING,
    [DATE]
)
VALUES
(
    @UserId,
    @StartDateTime,
    @EndDateTime,
    @LeaveId,
    @Reason,
    @RegisteredAt
);";

        const string logSql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0, LEFT(@LogDescr, 50));";

        await using var connection = new SqlConnection(_connectionString);
        var users = (await connection.QueryAsync<JustificarEmpleadoSelectedEmployee>(
            usersSql, new { UserIds = request.UserIds.Distinct().ToArray() })).ToList();

        var progress = new List<JustificarEmpleadoProgressItemViewModel>();
        var unit = (await GetExcepcionByIdAsync(request.LeaveId))?.Unit ?? 0;
        if (unit is < 1 or > 3)
        {
            return new JustificarEmpleadoAssignResult
            {
                Success = false,
                Message = "La excepción seleccionada no tiene una unidad válida.",
                ProgressItems = progress
            };
        }

        var startDate = request.StartDate!.Value.Date;
        var endDate = request.EndDate!.Value.Date;
        var startTime = request.StartTime is null ? TimeSpan.Zero : TimeSpan.Parse(request.StartTime);
        var endTime = request.EndTime is null ? TimeSpan.Zero : TimeSpan.Parse(request.EndTime);

        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var progressItem = new JustificarEmpleadoProgressItemViewModel
            {
                Position = index + 1,
                Total = users.Count,
                UserId = user.UserId,
                EmployeeName = user.Name ?? string.Empty,
                BadgeNumber = user.BadgeNumber,
                Status = "Iniciando",
                Result = "Pendiente"
            };
            progress.Add(progressItem);

            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                progressItem.Status = "Preparando asignación";
                var registeredAt = DateTime.Now;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    DateTime rowStart;
                    DateTime rowEnd;

                    if (unit == 3)
                    {
                        rowStart = date;
                        rowEnd = date.AddHours(23).AddMinutes(59);
                    }
                    else
                    {
                        rowStart = date.Add(startTime);
                        rowEnd = date.Add(endTime);
                    }

                    if (rowStart > rowEnd)
                    {
                        throw new InvalidOperationException("La hora de inicio no puede ser mayor que la hora de fin.");
                    }

                    await connection.ExecuteAsync(
                        duplicateSql,
                        new
                        {
                            UserId = user.UserId,
                            StartDateTime = rowStart,
                            LeaveId = request.LeaveId
                        },
                        transaction);

                    await connection.ExecuteAsync(
                        insertSql,
                        new
                        {
                            UserId = user.UserId,
                            StartDateTime = rowStart,
                            EndDateTime = rowEnd,
                            LeaveId = request.LeaveId,
                            Reason = request.Reason,
                            RegisteredAt = registeredAt
                        },
                        transaction);
                }

                await connection.ExecuteAsync(
                    logSql,
                    new
                    {
                        Operator = operatorName,
                        MachineAlias = machineAlias,
                        LogDescr = $"Justifica Empleado: {user.Name ?? user.BadgeNumber} - LeaveId {request.LeaveId}"
                    },
                    transaction);

                await transaction.CommitAsync();

                progressItem.Status = "Completado";
                progressItem.Result = "Asignación registrada correctamente.";
                progressItem.Success = true;
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync();
                progressItem.Status = "Error";
                progressItem.Result = $"Error SQL: {ex.Message}";

                return new JustificarEmpleadoAssignResult
                {
                    Success = false,
                    Message = $"No fue posible completar la asignación para {user.Name ?? user.BadgeNumber}.",
                    ProgressItems = progress
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                progressItem.Status = "Error";
                progressItem.Result = ex.Message;

                return new JustificarEmpleadoAssignResult
                {
                    Success = false,
                    Message = $"No fue posible completar la asignación para {user.Name ?? user.BadgeNumber}.",
                    ProgressItems = progress
                };
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        return new JustificarEmpleadoAssignResult
        {
            Success = true,
            Message = $"Asignación completada para {users.Count} persona(s).",
            ProgressItems = progress
        };
    }

    public async Task<JustificarEmpleadoDeleteConfirmViewModel?> GetDeleteConfirmationAsync(
        int userId,
        IReadOnlyList<JustificarEmpleadoDeleteItemRequest> items)
    {
        var employee = await GetEmployeeByIdAsync(userId);
        if (employee is null) return null;

        var assignments = await GetAsignacionesByUserAsync(userId);
        var selected = assignments.Where(a => items.Any(x =>
            x.LeaveId == a.LeaveId &&
            x.StartDateTime == a.StartDateTime &&
            x.EndDateTime == a.EndDateTime)).ToList();

        return new JustificarEmpleadoDeleteConfirmViewModel
        {
            UserId = userId,
            Ssn = employee.Ssn ?? string.Empty,
            EmployeeName = employee.Name ?? employee.BadgeNumber,
            Items = selected
        };
    }

    public async Task<OperationResult> DeleteExcepcionesAsync(
        JustificarEmpleadoDeleteRequest request,
        string operatorName,
        string machineAlias)
    {
        const string deleteSql = @"
DELETE FROM dbo.USER_SPEDAY
WHERE USERID = @UserId
  AND STARTSPECDAY = @StartDateTime
  AND DATEID = @LeaveId
  AND
  (
      (ENDSPECDAY = @EndDateTime)
      OR (ENDSPECDAY IS NULL AND @EndDateTime IS NULL)
  );";

        const string logSql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0, LEFT(@LogDescr, 50));";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var item in request.Items)
            {
                var rows = await connection.ExecuteAsync(
                    deleteSql,
                    new
                    {
                        UserId = request.UserId,
                        item.LeaveId,
                        item.StartDateTime,
                        item.EndDateTime
                    },
                    transaction);

                if (rows == 0)
                {
                    throw new InvalidOperationException(
                        "Una de las asignaciones seleccionadas ya no existe.");
                }
            }

            await connection.ExecuteAsync(
                logSql,
                new
                {
                    Operator = operatorName,
                    MachineAlias = machineAlias,
                    LogDescr = $"Borra Excepcion : USERID={request.UserId}, regs={request.Items.Count}"
                },
                transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Asignación(es) eliminada(s) correctamente.");
        }
        catch (SqlException Ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail("No fue posible eliminar las asignaciones por un error de base de datos ("+Ex.Message+"|"+operatorName+"|"+machineAlias+").");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail(ex.Message);
        }
    }

    private sealed class JustificarEmpleadoSelectedEmployee
    {
        public int UserId { get; set; }
        public string BadgeNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
