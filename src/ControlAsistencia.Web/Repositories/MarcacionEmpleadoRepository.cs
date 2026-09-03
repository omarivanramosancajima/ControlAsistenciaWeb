using System.Globalization;
using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class MarcacionEmpleadoRepository : IMarcacionEmpleadoRepository
{
    private readonly string _connectionString;

    public MarcacionEmpleadoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync()
    {
        const string sql = @"
WITH DepartmentTree AS
(
    SELECT DEPTID AS DeptId, DEPTNAME AS DeptName, SUPDEPTID AS SupDeptId,
           0 AS [Level], CAST(ISNULL(DEPTNAME, '') AS VARCHAR(500)) AS HierarchyName
    FROM dbo.DEPARTMENTS WITH (NOLOCK)
    WHERE SUPDEPTID = 0
    UNION ALL
    SELECT D.DEPTID, D.DEPTNAME, D.SUPDEPTID, T.[Level] + 1,
           CAST(T.HierarchyName + ' > ' + ISNULL(D.DEPTNAME, '') AS VARCHAR(500))
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    INNER JOIN DepartmentTree T ON T.DeptId = D.SUPDEPTID
)
SELECT DeptId, DeptName, SupDeptId, [Level], HierarchyName
FROM DepartmentTree
ORDER BY HierarchyName
OPTION (MAXRECURSION 100);";

        await using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<DepartmentDTO>(sql)).ToList();
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.DEPARTMENTS WITH (NOLOCK) WHERE DEPTID = @DeptId;";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { DeptId = deptId }) > 0;
    }

    public async Task<IReadOnlyList<MarcacionEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(
        int deptId, bool includeSubDependencies)
    {
        const string sql = @"
WITH DepartmentScope AS
(
    SELECT DEPTID FROM dbo.DEPARTMENTS WITH (NOLOCK) WHERE DEPTID = @DeptId
    UNION ALL
    SELECT D.DEPTID
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    INNER JOIN DepartmentScope DS ON DS.DEPTID = D.SUPDEPTID
)
SELECT U.USERID AS UserId, U.BADGENUMBER AS BadgeNumber, U.SSN AS Ssn,
       U.NAME AS Name, U.DEFAULTDEPTID AS DefaultDeptId,
       D.DEPTNAME AS DepartmentName, CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
       U.PRIVILEGE AS Privilege
FROM dbo.USERINFO U WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE (@IncludeSubDependencies = 1 AND U.DEFAULTDEPTID IN (SELECT DEPTID FROM DepartmentScope))
   OR (@IncludeSubDependencies = 0 AND U.DEFAULTDEPTID = @DeptId)
ORDER BY U.BADGENUMBER ASC
OPTION (MAXRECURSION 100);";

        await using var connection = new SqlConnection(_connectionString);
        var items = (await connection.QueryAsync<MarcacionEmpleadoEmployeeItemViewModel>(
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

    public async Task<MarcacionEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId)
    {
        const string sql = @"
SELECT TOP (1) USERID AS UserId, BADGENUMBER AS BadgeNumber, SSN AS Ssn,
       NAME AS Name, DEFAULTDEPTID AS DefaultDeptId
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<MarcacionEmpleadoEmployeeItemViewModel>(
            sql, new { UserId = userId });
    }

    public async Task<(IReadOnlyList<MarcacionEmpleadoMarcacionItemViewModel> Items, bool HasNextPage)> GetMarcacionesByUserAsync(int userId, int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100000);
        var offset = (pageNumber - 1) * pageSize;
        const string sql = @"
;WITH LatestExact AS
(
    SELECT E.*,
           ROW_NUMBER() OVER
           (
               PARTITION BY E.USERID, E.CHECKTIME
               ORDER BY E.[DATE] DESC, E.EXACTID DESC
           ) AS rn
    FROM dbo.CHECKEXACT E WITH (NOLOCK)
    WHERE E.USERID = @UserId
),
CurrentMarks AS
(
    SELECT
        C.USERID AS UserId,
        C.CHECKTIME AS CheckTime,
        ISNULL(C.CHECKTYPE, '') AS CheckType,
        C.VERIFYCODE AS VerifyCode,
        C.SENSORID AS SensorId,
        C.Memoinfo AS MemoInfo,
        C.WorkCode AS WorkCode,
        C.sn AS SerialNumber,
        C.UserExtFmt AS UserExtFmt,
        E.ISADD AS IsAdd,
        E.YUYIN AS Reason,
        E.MODIFYBY AS ModifiedBy,
        E.[DATE] AS RegisteredAt,
        CAST(CASE WHEN E.ISADD = 1 THEN 1 ELSE 0 END AS bit) AS CanDelete,
        CASE WHEN E.ISADD = 1 THEN 'Manual Agregado' ELSE 'De Equipo' END AS RecordType
    FROM dbo.CHECKINOUT C WITH (NOLOCK)
    LEFT JOIN LatestExact E
      ON E.USERID = C.USERID
     AND E.CHECKTIME = C.CHECKTIME
     AND E.rn = 1
    WHERE C.USERID = @UserId
      AND (E.ISADD IS NULL OR E.ISADD = 1)
),
DeletedMarks AS
(
    SELECT
        E.USERID AS UserId,
        E.CHECKTIME AS CheckTime,
        ISNULL(E.CHECKTYPE, '') AS CheckType,
        CAST(NULL AS int) AS VerifyCode,
        CAST(NULL AS varchar(5)) AS SensorId,
        CAST(NULL AS varchar(30)) AS MemoInfo,
        CAST(NULL AS varchar(24)) AS WorkCode,
        CAST(NULL AS varchar(20)) AS SerialNumber,
        CAST(NULL AS int) AS UserExtFmt,
        E.ISADD AS IsAdd,
        E.YUYIN AS Reason,
        E.MODIFYBY AS ModifiedBy,
        E.[DATE] AS RegisteredAt,
        CAST(0 AS bit) AS CanDelete,
        CASE WHEN E.ISADD = 0 THEN 'Manual Borrado'
             WHEN E.ISADD = 2 THEN 'De Equipo Borrado'
             ELSE 'Registro Eliminado' END AS RecordType
    FROM LatestExact E
    WHERE E.rn = 1
      AND E.USERID = @UserId
      AND E.ISADD IN (0, 2)
)
SELECT *
FROM
(
    SELECT * FROM CurrentMarks
    UNION ALL
    SELECT * FROM DeletedMarks
) M
ORDER BY M.CheckTime DESC, M.RegisteredAt DESC
OFFSET @Offset ROWS FETCH NEXT @FetchSize ROWS ONLY;";

        await using var connection = new SqlConnection(_connectionString);
        var rows = (await connection.QueryAsync<MarcacionEmpleadoMarcacionItemViewModel>(
            sql, new { UserId = userId, Offset = offset, FetchSize = pageSize + 1 })).ToList();

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
            rows.RemoveAt(rows.Count - 1);

        return (rows, hasNextPage);
    }

    public async Task<MarcacionEmpleadoRegisterResult> RegisterManualMarkAsync(
        MarcacionEmpleadoRegisterRequest request, string operatorName, string machineAlias)
    {
        const string userSql = @"
SELECT USERID AS UserId, BADGENUMBER AS BadgeNumber, NAME AS Name
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID IN @UserIds
ORDER BY BADGENUMBER ASC;";

        const string existsSql = @"
SELECT COUNT(1)
FROM dbo.CHECKINOUT WITH (NOLOCK)
WHERE USERID = @UserId AND CHECKTIME = @CheckTime;";

        const string insertCheckSql = @"
INSERT INTO dbo.CHECKINOUT
(USERID, CHECKTIME, CHECKTYPE, VERIFYCODE, SENSORID, Memoinfo, WorkCode, sn, UserExtFmt)
VALUES (@UserId, @CheckTime, 'I', 0, NULL, NULL, '0', NULL, NULL);";

        const string insertExactSql = @"
INSERT INTO dbo.CHECKEXACT
(USERID, CHECKTIME, CHECKTYPE, ISADD, YUYIN, ISMODIFY, ISDELETE, INCOUNT, ISCOUNT, MODIFYBY, [DATE])
VALUES
(@UserId, @CheckTime, 'I', 1, @Reason, 0, 0, 0, 0, @Operator, @RegisteredAt);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var users = (await connection.QueryAsync<SelectedEmployee>(
            userSql, new { request.UserIds })).ToList();

        var progress = new List<MarcacionEmpleadoProgressItemViewModel>();
        var checkDateTime = request.CheckDate!.Value.Date.Add(TimeSpan.ParseExact(
            request.CheckTime!, @"hh\:mm", CultureInfo.InvariantCulture));
        var registeredAt = DateTime.Now;

        for (var i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var p = new MarcacionEmpleadoProgressItemViewModel
            {
                Position = i + 1, Total = users.Count, UserId = user.UserId,
                EmployeeName = user.Name ?? string.Empty, BadgeNumber = user.BadgeNumber,
                Status = "Iniciando"
            };
            progress.Add(p);

            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var exists = await connection.ExecuteScalarAsync<int>(
                    existsSql, new { UserId = user.UserId, CheckTime = checkDateTime }, transaction);

                if (exists > 0)
                {
                    p.Status = "Completado";
                    p.Result = "1 marca NO agregada: ya existe una marcación para esa fecha y hora.";
                    p.Success = true;
                    await transaction.CommitAsync();
                    continue;
                }

                await connection.ExecuteAsync(insertCheckSql,
                    new { UserId = user.UserId, CheckTime = checkDateTime }, transaction);

                await connection.ExecuteAsync(insertExactSql,
                    new
                    {
                        UserId = user.UserId,
                        CheckTime = checkDateTime,
                        Reason = request.Reason,
                        Operator = operatorName,
                        RegisteredAt = registeredAt
                    }, transaction);

                await transaction.CommitAsync();
                p.Status = "Completado";
                p.Result = "1 marca agregada.";
                p.Success = true;
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync();
                p.Status = "Error";
                p.Result = $"Error SQL: {ex.Message}";
                return new MarcacionEmpleadoRegisterResult
                {
                    Success = false,
                    Message = $"No fue posible registrar la marca para {user.Name ?? user.BadgeNumber} ("+ex.Message+").",
                    ProgressItems = progress
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                p.Status = "Error";
                p.Result = ex.Message;
                return new MarcacionEmpleadoRegisterResult
                {
                    Success = false,
                    Message = $"No fue posible registrar la marca para {user.Name ?? user.BadgeNumber} ["+ex.Message+"].",
                    ProgressItems = progress
                };
            }
        }

        return new MarcacionEmpleadoRegisterResult
        {
            Success = true,
            Message = $"Proceso terminado para {users.Count} persona(s).",
            ProgressItems = progress
        };
    }

    public async Task<MarcacionEmpleadoDeleteConfirmViewModel?> GetDeleteConfirmationAsync(
        int userId, IReadOnlyList<MarcacionEmpleadoDeleteItemRequest> items)
    {
        var employee = await GetEmployeeByIdAsync(userId);
        if (employee is null) return null;

        var marks = await GetMarcacionesByUserAsync(userId, 1, 100000);
        var selected = marks.Items.Where(x => x.CanDelete &&
            items.Any(i => i.CheckTime == x.CheckTime)).ToList();

        if (selected.Count != items.Count) return null;

        return new MarcacionEmpleadoDeleteConfirmViewModel
        {
            UserId = userId,
            Ssn = employee.Ssn ?? string.Empty,
            EmployeeName = employee.Name ?? employee.BadgeNumber,
            Items = selected.ToList()
        };
    }

    public async Task<OperationResult> DeleteMarcacionesAsync(
        MarcacionEmpleadoDeleteRequest request, string operatorName, string machineAlias)
    {
        const string currentSql = @"
SELECT TOP (1)
    C.USERID AS UserId, C.CHECKTIME AS CheckTime,
    E.ISADD AS IsAdd
FROM dbo.CHECKINOUT C
OUTER APPLY
(
    SELECT TOP (1) X.ISADD
    FROM dbo.CHECKEXACT X WITH (NOLOCK)
    WHERE X.USERID = C.USERID
      AND X.CHECKTIME = C.CHECKTIME
    ORDER BY X.[DATE] DESC, X.EXACTID DESC
) E
WHERE C.USERID = @UserId
  AND C.CHECKTIME = @CheckTime;";

        const string insertExactSql = @"
INSERT INTO dbo.CHECKEXACT
(USERID, CHECKTIME, CHECKTYPE, ISADD, YUYIN, ISMODIFY, ISDELETE, INCOUNT, ISCOUNT, MODIFYBY, [DATE])
VALUES
(@UserId, @CheckTime, @CheckType, @IsAdd, @Reason, 0, 1, 0, 0, @Operator, @RegisteredAt);";

        const string deleteSql = @"
DELETE FROM dbo.CHECKINOUT
WHERE USERID = @UserId AND CHECKTIME = @CheckTime;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var item in request.Items)
            {
                var current = await connection.QuerySingleOrDefaultAsync<CurrentMark>(
                    currentSql,
                    new { request.UserId, item.CheckTime }, transaction);

                if (current is null)
                    throw new InvalidOperationException("Una de las marcaciones seleccionadas ya no existe.");

                if (current.IsAdd is 0 or 2)
                    throw new InvalidOperationException("Una de las marcaciones seleccionadas ya fue borrada.");

                var isManual = current.IsAdd == 1;
                var isAddAudit = (short)(isManual ? 0 : 2);
                var reason = isManual
                    ? "Borrado de Marca Manual"
                    : "Borrado Marca De Equipo";

                await connection.ExecuteAsync(
                    insertExactSql,
                    new
                    {
                        UserId = request.UserId,
                        CheckTime = item.CheckTime,
                        CheckType = "I",
                        IsAdd = isAddAudit,
                        Reason = reason,
                        Operator = operatorName,
                        RegisteredAt = DateTime.Now
                    }, transaction);

                var rows = await connection.ExecuteAsync(
                    deleteSql,
                    new { request.UserId, item.CheckTime }, transaction);

                if (rows != 1)
                    throw new InvalidOperationException("No fue posible eliminar una de las marcaciones seleccionadas.");
            }

            await connection.ExecuteAsync(@"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, @LogDescr);",
                new
                {
                    Operator = operatorName,
                    MachineAlias = machineAlias,
                    LogDescr = $"Borra Marcacion: USERID={request.UserId}, regs={request.Items.Count}"
                }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Marcación(es) eliminada(s) correctamente.");
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail($"No fue posible eliminar las marcaciones por un error de base de datos: {ex.Message}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail(ex.Message);
        }
    }

    private sealed class SelectedEmployee
    {
        public int UserId { get; set; }
        public string BadgeNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class CurrentMark
    {
        public int UserId { get; set; }
        public DateTime CheckTime { get; set; }
        public short? IsAdd { get; set; }
    }
}