using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class TurnosEmpleadoRepository : ITurnosEmpleadoRepository
{
    private readonly string _connectionString;

    public TurnosEmpleadoRepository(IConfiguration configuration)
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
            var items = await connection.QueryAsync<DepartmentDTO>(sql);
            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las áreas.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las áreas.", ex);
        }
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.DEPARTMENTS WITH (NOLOCK) WHERE DEPTID = @DeptId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { DeptId = deptId }) > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al validar el área seleccionada.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar el área seleccionada.", ex);
        }
    }

    public async Task<IReadOnlyList<TurnoEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(int deptId, bool includeSubDependencies)
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

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = (await connection.QueryAsync<TurnoEmpleadoEmployeeItemViewModel>(sql, new
            {
                DeptId = deptId,
                IncludeSubDependencies = includeSubDependencies
            })).ToList();

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
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las personas del área.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las personas del área.", ex);
        }
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.USERINFO WITH (NOLOCK) WHERE USERID = @UserId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId }) > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al validar la persona seleccionada.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar la persona seleccionada.", ex);
        }
    }

    public async Task<TurnoEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId)
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

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var item = await connection.QueryFirstOrDefaultAsync<TurnoEmpleadoEmployeeItemViewModel>(sql, new { UserId = userId });
            if (item is null)
            {
                return null;
            }

            item.PrivilegeDescription = PrivilegeHelper.GetDescription(item.Privilege);
            item.PhotoBase64 = item.Photo is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                : null;
            return item;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener la persona seleccionada.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener la persona seleccionada.", ex);
        }
    }

    public async Task<IReadOnlyList<TurnoEmpleadoAsignacionItemViewModel>> GetAsignacionesByUserAsync(int userId)
    {
        const string sql = @"
SELECT
    UOR.USERID AS UserId,
    UOR.NUM_OF_RUN_ID AS NumOfRunId,
    UOR.STARTDATE AS StartDate,
    UOR.ENDDATE AS EndDate,
    UOR.ISNOTOF_RUN AS IsNotOfRun,
    UOR.ORDER_RUN AS OrderRun,
    NR.NAME AS TurnoName,
    NR.STARTDATE AS TurnoStartDate,
    NR.ENDDATE AS TurnoEndDate,
    NR.CYLE AS Cyle,
    NR.UNITS AS Units
FROM dbo.USER_OF_RUN UOR WITH (NOLOCK)
INNER JOIN dbo.NUM_RUN NR WITH (NOLOCK) ON NR.NUM_RUNID = UOR.NUM_OF_RUN_ID
WHERE UOR.USERID = @UserId
ORDER BY UOR.ENDDATE DESC, UOR.STARTDATE DESC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<TurnoEmpleadoAsignacionItemViewModel>(sql, new { UserId = userId });
            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las asignaciones del empleado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener las asignaciones del empleado.", ex);
        }
    }

    public async Task<TurnoEmpleadoAsignacionItemViewModel?> GetAsignacionByKeyAsync(int userId, int numOfRunId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
SELECT TOP (1)
    UOR.USERID AS UserId,
    UOR.NUM_OF_RUN_ID AS NumOfRunId,
    UOR.STARTDATE AS StartDate,
    UOR.ENDDATE AS EndDate,
    UOR.ISNOTOF_RUN AS IsNotOfRun,
    UOR.ORDER_RUN AS OrderRun,
    NR.NAME AS TurnoName,
    NR.STARTDATE AS TurnoStartDate,
    NR.ENDDATE AS TurnoEndDate,
    NR.CYLE AS Cyle,
    NR.UNITS AS Units
FROM dbo.USER_OF_RUN UOR WITH (NOLOCK)
INNER JOIN dbo.NUM_RUN NR WITH (NOLOCK) ON NR.NUM_RUNID = UOR.NUM_OF_RUN_ID
WHERE UOR.USERID = @UserId
  AND UOR.NUM_OF_RUN_ID = @NumOfRunId
  AND UOR.STARTDATE = @StartDate
  AND UOR.ENDDATE = @EndDate;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TurnoEmpleadoAsignacionItemViewModel>(sql, new
            {
                UserId = userId,
                NumOfRunId = numOfRunId,
                StartDate = startDate,
                EndDate = endDate
            });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener la asignación seleccionada.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener la asignación seleccionada.", ex);
        }
    }

    public async Task<IReadOnlyList<TurnoDTO>> GetTurnosAsync()
    {
        const string sql = @"
SELECT NUM_RUNID, NAME, STARTDATE, ENDDATE, CYLE, UNITS
FROM dbo.NUM_RUN WITH (NOLOCK)
ORDER BY NAME ASC;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<TurnoDTO>(sql);
            return items.ToList();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener los turnos.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener los turnos.", ex);
        }
    }

    public async Task<bool> TurnoExistsAsync(int numRunId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.NUM_RUN WITH (NOLOCK) WHERE NUM_RUNID = @NumRunId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { NumRunId = numRunId }) > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al validar el turno seleccionado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar el turno seleccionado.", ex);
        }
    }

    public async Task<TurnosEmpleadoAssignResult> AssignTurnoAsync(TurnosEmpleadoAssignRequest request, string operatorName, string machineAlias)
    {
        const string usersSql = @"
SELECT USERID AS UserId, BADGENUMBER AS BadgeNumber, NAME AS Name
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID IN @UserIds
ORDER BY BADGENUMBER ASC;";
        const string overlappingSql = @"
SELECT USERID AS UserId,
       NUM_OF_RUN_ID AS NumOfRunId,
       STARTDATE AS StartDate,
       ENDDATE AS EndDate,
       ISNOTOF_RUN AS IsNotOfRun,
       ORDER_RUN AS OrderRun
FROM dbo.USER_OF_RUN
WHERE USERID = @UserId
  AND STARTDATE <= @NewEndDate
  AND ENDDATE >= @NewStartDate
ORDER BY STARTDATE ASC, ENDDATE ASC;";
        const string deleteOverlapSql = @"
DELETE FROM dbo.USER_OF_RUN
WHERE USERID = @UserId
  AND NUM_OF_RUN_ID = @NumOfRunId
  AND STARTDATE = @StartDate
  AND ENDDATE = @EndDate;";
        const string insertSql = @"
INSERT INTO dbo.USER_OF_RUN (USERID, NUM_OF_RUN_ID, STARTDATE, ENDDATE, ISNOTOF_RUN, ORDER_RUN)
VALUES (@UserId, @NumOfRunId, @StartDate, @EndDate, @IsNotOfRun, @OrderRun);";
        const string logSql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, @LogDescr);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var users = (await connection.QueryAsync<TurnoEmpleadoSelectedEmployeeViewModel>(usersSql, new { UserIds = request.UserIds.Distinct().ToArray() })).ToList();
            var progress = new List<TurnosEmpleadoProgressItemViewModel>();
            var newStartDate = request.StartDate!.Value;
            var newEndDate = request.EndDate!.Value;

            for (var index = 0; index < users.Count; index++)
            {
                var user = users[index];
                var progressItem = new TurnosEmpleadoProgressItemViewModel
                {
                    Position = index + 1,
                    Total = users.Count,
                    UserId = user.UserId,
                    EmployeeName = user.Name ?? string.Empty,
                    BadgeNumber = user.BadgeNumber,
                    Status = "Iniciando",
                    Result = "Pendiente",
                    Success = false
                };

                progress.Add(progressItem);

                try
                {
                    await connection.OpenAsync();
                    await using var transaction = await connection.BeginTransactionAsync();

                    progressItem.Status = "Consultando asignaciones";
                    var overlaps = (await connection.QueryAsync<TurnoEmpleadoAsignacionItemViewModel>(overlappingSql, new
                    {
                        UserId = user.UserId,
                        NewStartDate = newStartDate,
                        NewEndDate = newEndDate
                    }, transaction)).ToList();

                    progressItem.Status = "Resolviendo rangos";
                    foreach (var overlap in overlaps)
                    {
                        await connection.ExecuteAsync(deleteOverlapSql, new
                        {
                            UserId = overlap.UserId,
                            NumOfRunId = overlap.NumOfRunId,
                            StartDate = overlap.StartDate,
                            EndDate = overlap.EndDate
                        }, transaction);

                        if (overlap.StartDate < newStartDate)
                        {
                            var leftEnd = newStartDate.AddDays(-1);
                            if (overlap.StartDate <= leftEnd)
                            {
                                await connection.ExecuteAsync(insertSql, new
                                {
                                    UserId = overlap.UserId,
                                    NumOfRunId = overlap.NumOfRunId,
                                    StartDate = overlap.StartDate,
                                    EndDate = leftEnd,
                                    IsNotOfRun = overlap.IsNotOfRun ?? (short)0,
                                    OrderRun = overlap.OrderRun
                                }, transaction);
                            }
                        }

                        if (overlap.EndDate > newEndDate)
                        {
                            var rightStart = newEndDate.AddDays(1);
                            if (rightStart <= overlap.EndDate)
                            {
                                await connection.ExecuteAsync(insertSql, new
                                {
                                    UserId = overlap.UserId,
                                    NumOfRunId = overlap.NumOfRunId,
                                    StartDate = rightStart,
                                    EndDate = overlap.EndDate,
                                    IsNotOfRun = overlap.IsNotOfRun ?? (short)0,
                                    OrderRun = overlap.OrderRun
                                }, transaction);
                            }
                        }
                    }

                    progressItem.Status = "Guardando";
                    await connection.ExecuteAsync(insertSql, new
                    {
                        UserId = user.UserId,
                        NumOfRunId = request.NumRunId!.Value,
                        StartDate = newStartDate,
                        EndDate = newEndDate,
                        IsNotOfRun = (short)0,
                        OrderRun = (int?)null
                    }, transaction);

                    await connection.ExecuteAsync(logSql, new
                    {
                        Operator = operatorName,
                        MachineAlias = machineAlias,
                        LogDescr = $"Programa Turno Empleado: {user.Name ?? user.BadgeNumber}"
                    }, transaction);

                    await transaction.CommitAsync();
                    progressItem.Status = "Completado";
                    progressItem.Result = "Asignación registrada correctamente.";
                    progressItem.Success = true;
                }
                catch (SqlException ex)
                {
                    progressItem.Status = "Error";
                    progressItem.Result = $"Error SQL: {ex.Message}";
                    return new TurnosEmpleadoAssignResult
                    {
                        Success = false,
                        Message = $"No fue posible completar la asignación para {user.Name ?? user.BadgeNumber}.",
                        ProgressItems = progress
                    };
                }
                catch (Exception ex)
                {
                    progressItem.Status = "Error";
                    progressItem.Result = ex.Message;
                    return new TurnosEmpleadoAssignResult
                    {
                        Success = false,
                        Message = $"No fue posible completar la asignación para {user.Name ?? user.BadgeNumber}.",
                        ProgressItems = progress
                    };
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
            }

            return new TurnosEmpleadoAssignResult
            {
                Success = true,
                Message = "Asignación registrada correctamente.",
                ProgressItems = progress
            };
        }
        catch (SqlException)
        {
            return new TurnosEmpleadoAssignResult
            {
                Success = false,
                Message = "No fue posible procesar la asignación de turnos.",
                ProgressItems = []
            };
        }
        catch (Exception)
        {
            return new TurnosEmpleadoAssignResult
            {
                Success = false,
                Message = "No fue posible procesar la asignación de turnos.",
                ProgressItems = []
            };
        }
    }

    public async Task<OperationResult> DeleteAsignacionAsync(TurnosEmpleadoDeleteRequest request, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @TurnoName VARCHAR(100);
SELECT @TurnoName = NR.NAME
FROM dbo.USER_OF_RUN UOR
INNER JOIN dbo.NUM_RUN NR ON NR.NUM_RUNID = UOR.NUM_OF_RUN_ID
WHERE UOR.USERID = @UserId
  AND UOR.NUM_OF_RUN_ID = @NumOfRunId
  AND UOR.STARTDATE = @StartDate
  AND UOR.ENDDATE = @EndDate;

DELETE FROM dbo.USER_OF_RUN
WHERE USERID = @UserId
  AND NUM_OF_RUN_ID = @NumOfRunId
  AND STARTDATE = @StartDate
  AND ENDDATE = @EndDate;

IF @@ROWCOUNT = 0
BEGIN
    RAISERROR('La asignación ya no existe o no corresponde a la persona seleccionada.', 16, 1);
END

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Turno Empleado: ' + ISNULL(@TurnoName, ''));";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                UserId = request.UserId,
                NumOfRunId = request.NumOfRunId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Operator = operatorName,
                MachineAlias = machineAlias
            });
            return OperationResult.Ok("Asignación eliminada correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail(ex.Message.Contains("La asignación ya no existe", StringComparison.OrdinalIgnoreCase)
                ? "La asignación ya no existe o no corresponde a la persona seleccionada."
                : "No fue posible eliminar la asignación por un error de base de datos.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible eliminar la asignación en este momento.");
        }
    }
}