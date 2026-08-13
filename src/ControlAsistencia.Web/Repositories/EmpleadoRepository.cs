using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly string _connectionString;

    private static readonly IReadOnlyDictionary<string, string> DependencyMessages = new Dictionary<string, string>
    {
        ["TEMPLATE"] = "huellas registradas",
        ["FaceTemp"] = "rostros registrados",
        ["CHECKINOUT"] = "asistencias registradas",
        ["CHECKEXACT"] = "marcas manuales registradas",
        ["USER_TEMP_SCH"] = "horarios extraordinarios registrados",
        ["USER_SPEDAY"] = "permisos o justificaciones registrados",
        ["USER_OF_RUN"] = "turnos registrados",
        ["SECURITYDETAILS"] = "detalles de seguridad registrados"
    };

    public EmpleadoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<(IReadOnlyList<EmpleadoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
SELECT
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.GENDER AS Gender,
    U.TITLE AS Title,
    U.PAGER AS Pager,
    U.BIRTHDAY AS Birthday,
    U.HIREDDAY AS HiredDay,
    U.STREET AS Street,
    U.OPHONE AS OPhone,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    U.MINZU AS Minzu,
    U.MVerifyPass AS MVerifyPass,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
    U.privilege AS Privilege,
    U.CardNo AS CardNo
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
ORDER BY U.USERID DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.USERINFO WITH (NOLOCK);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (pageNumber - 1) * pageSize,
                PageSize = pageSize
            });

            var items = (await multi.ReadAsync<EmpleadoDTO>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();

            foreach (var item in items)
            {
                item.PrivilegeDescription = GetPrivilegeDescription(item.Privilege);
                item.PhotoBase64 = item.Photo is { Length: > 0 }
                    ? $"data:image/jpeg;base64,{Convert.ToBase64String(item.Photo)}"
                    : null;
            }

            return (items, totalRecords);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener el listado de empleados.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener el listado de empleados.", ex);
        }
    }

    public async Task<EmpleadoDTO?> GetByIdAsync(int userId)
    {
        const string sql = @"
SELECT TOP (1)
    U.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    U.SSN AS Ssn,
    U.NAME AS Name,
    U.GENDER AS Gender,
    U.TITLE AS Title,
    U.PAGER AS Pager,
    U.BIRTHDAY AS Birthday,
    U.HIREDDAY AS HiredDay,
    U.STREET AS Street,
    U.OPHONE AS OPhone,
    U.DEFAULTDEPTID AS DefaultDeptId,
    D.DEPTNAME AS DepartmentName,
    U.MINZU AS Minzu,
    U.MVerifyPass AS MVerifyPass,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo,
    U.privilege AS Privilege,
    U.CardNo AS CardNo
FROM dbo.USERINFO U WITH (NOLOCK)
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
WHERE U.USERID = @UserId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var empleado = await connection.QueryFirstOrDefaultAsync<EmpleadoDTO>(sql, new { UserId = userId });

            if (empleado is null)
            {
                return null;
            }

            empleado.PrivilegeDescription = GetPrivilegeDescription(empleado.Privilege);
            empleado.PhotoBase64 = empleado.Photo is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(empleado.Photo)}"
                : null;

            return empleado;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener el empleado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener el empleado.", ex);
        }
    }

    public async Task<bool> ExistsBadgeNumberAsync(string badgeNumber, int? excludeUserId = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE BADGENUMBER = @BadgeNumber
  AND (@ExcludeUserId IS NULL OR USERID <> @ExcludeUserId);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                BadgeNumber = badgeNumber,
                ExcludeUserId = excludeUserId
            });

            return count > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al validar el código del empleado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar el código del empleado.", ex);
        }
    }

    public async Task<bool> DepartmentExistsAsync(int deptId)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.DEPARTMENTS WITH (NOLOCK) WHERE DEPTID = @DeptId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new { DeptId = deptId });
            return count > 0;
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

    public async Task RegisterViewAuditAsync(string employeeName, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Visualiza Persona: ' + @EmployeeName);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                Operator = operatorName,
                MachineAlias = machineAlias,
                EmployeeName = employeeName
            });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al registrar la auditoría de visualización.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al registrar la auditoría de visualización.", ex);
        }
    }

    public async Task<OperationResult> CreateAsync(EmpleadoFormViewModel model, byte[]? photoBytes, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.USERINFO
(
    BADGENUMBER, SSN, NAME, GENDER, TITLE, PAGER, BIRTHDAY, HIREDDAY, STREET,
    CITY, STATE, ZIP, OPHONE, FPHONE, VERIFICATIONMETHOD, DEFAULTDEPTID,
    ATT, INLATE, OUTEARLY, OVERTIME, SEP, HOLIDAY, MINZU, LUNCHDURATION,
    MVerifyPass, PHOTO, Notes, privilege, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, MinAutoSchInterval, RegisterOT, InheritDeptRule, EMPRIVILEGE,
    CardNo, FaceGroup, AccGroup, UseAccGroupTZ, VerifyCode, Expires,
    ValidCount, ValidTimeBegin, ValidTimeEnd, TimeZone1, TimeZone2, TimeZone3, Pin1
)
VALUES
(
    @BadgeNumber, @Ssn, @Name, @Gender, @Title, @Pager, @Birthday, @HiredDay, @Street,
    NULL, NULL, NULL, @OPhone, NULL, NULL, @DefaultDeptId,
    1, 1, 1, 1, 1, 1, @Minzu, 1,
    @MVerifyPass, @Photo, NULL, @Privilege, 1, 1,
    1, 24, 1, 1, 1,
    @CardNo, 1, 1, 1, 0, 0,
    0, NULL, NULL, 1, 1, 1, NULL
);

DECLARE @NameLog VARCHAR(24) = @Name;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Agrega Persona: ' + @NameLog);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                model.BadgeNumber,
                model.Ssn,
                model.Name,
                model.Gender,
                model.Title,
                model.Pager,
                model.Birthday,
                model.HiredDay,
                model.Street,
                model.OPhone,
                model.DefaultDeptId,
                model.Minzu,
                model.MVerifyPass,
                Photo = photoBytes,
                model.Privilege,
                model.CardNo,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Empleado registrado correctamente.");
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible registrar el empleado por un error de base de datos.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible registrar el empleado en este momento.");
        }
    }

    public async Task<OperationResult> UpdateAsync(EmpleadoFormViewModel model, byte[]? photoBytes, string operatorName, string machineAlias)
    {
        var sql = @"
UPDATE dbo.USERINFO
SET BADGENUMBER = @BadgeNumber,
    SSN = @Ssn,
    NAME = @Name,
    GENDER = @Gender,
    TITLE = @Title,
    PAGER = @Pager,
    BIRTHDAY = @Birthday,
    HIREDDAY = @HiredDay,
    STREET = @Street,
    OPHONE = @OPhone,
    DEFAULTDEPTID = @DefaultDeptId,
    MINZU = @Minzu,
    MVerifyPass = @MVerifyPass,
    privilege = @Privilege,
    CardNo = @CardNo,
    PHOTO = @PhotoClause
WHERE USERID = @UserId;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Edita Persona: ' + @Name);";

        sql = sql.Replace("@PhotoClause", photoBytes is null ? "PHOTO" : "@Photo");

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                model.BadgeNumber,
                model.Ssn,
                model.Name,
                model.Gender,
                model.Title,
                model.Pager,
                model.Birthday,
                model.HiredDay,
                model.Street,
                model.OPhone,
                model.DefaultDeptId,
                model.Minzu,
                model.MVerifyPass,
                model.Privilege,
                model.CardNo,
                model.UserId,
                Photo = photoBytes,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Empleado actualizado correctamente.");
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible actualizar el empleado por un error de base de datos.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible actualizar el empleado en este momento.");
        }
    }

    public async Task<DeleteDependencyResult> ValidateDeleteAsync(int userId)
    {
        const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.TEMPLATE WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'TEMPLATE'
ELSE IF EXISTS (SELECT 1 FROM dbo.FaceTemp WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'FaceTemp'
ELSE IF EXISTS (SELECT 1 FROM dbo.CHECKINOUT WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'CHECKINOUT'
ELSE IF EXISTS (SELECT 1 FROM dbo.CHECKEXACT WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'CHECKEXACT'
ELSE IF EXISTS (SELECT 1 FROM dbo.USER_TEMP_SCH WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'USER_TEMP_SCH'
ELSE IF EXISTS (SELECT 1 FROM dbo.USER_SPEDAY WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'USER_SPEDAY'
ELSE IF EXISTS (SELECT 1 FROM dbo.USER_OF_RUN WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'USER_OF_RUN'
ELSE IF EXISTS (SELECT 1 FROM dbo.SECURITYDETAILS WITH (NOLOCK) WHERE USERID = @UserId) SELECT 'SECURITYDETAILS'
ELSE SELECT NULL;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var dependency = await connection.ExecuteScalarAsync<string?>(sql, new { UserId = userId });

            if (string.IsNullOrWhiteSpace(dependency))
            {
                return new DeleteDependencyResult { HasDependency = false };
            }

            var description = DependencyMessages.TryGetValue(dependency, out var value) ? value : dependency;
            return new DeleteDependencyResult
            {
                HasDependency = true,
                DependencyMessage = $"No se puede eliminar la persona porque tiene {description}."
            };
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al validar dependencias del empleado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar dependencias del empleado.", ex);
        }
    }

    public async Task<OperationResult> DeleteAsync(int userId, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @Name VARCHAR(24);
SELECT @Name = NAME FROM dbo.USERINFO WHERE USERID = @UserId;

DELETE FROM dbo.USERINFO WHERE USERID = @UserId;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Persona: ' + ISNULL(@Name, ''));";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { UserId = userId, Operator = operatorName, MachineAlias = machineAlias });
            return OperationResult.Ok("Empleado eliminado correctamente.");
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible eliminar el empleado por un error de base de datos.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible eliminar el empleado en este momento.");
        }
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

    public async Task<OperationResult> CreateDepartmentAsync(string deptName, int supDeptId)
    {
        const string sql = @"
INSERT INTO dbo.DEPARTMENTS
(
    DEPTNAME, SUPDEPTID, InheritParentSch, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, InLate, OutEarly, InheritDeptRule, MinAutoSchInterval,
    RegisterOT, DefaultSchId, ATT, Holiday, OverTime
)
VALUES
(
    @DeptName, @SupDeptId, 1, 1, 1,
    1, 1, 1, 1, 24,
    1, 1, 1, 1, 1
);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { DeptName = deptName, SupDeptId = supDeptId });
            return OperationResult.Ok("Área registrada correctamente.");
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible registrar el área por un error de base de datos.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible registrar el área en este momento.");
        }
    }

    public async Task<int> CreateAsync(EmpleadoDTO dto)
    {
        var model = new EmpleadoFormViewModel
        {
            BadgeNumber = dto.BadgeNumber,
            Ssn = dto.Ssn ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            Gender = dto.Gender ?? string.Empty,
            Title = dto.Title,
            Pager = dto.Pager,
            Birthday = dto.Birthday,
            HiredDay = dto.HiredDay,
            Street = dto.Street,
            OPhone = dto.OPhone,
            DefaultDeptId = dto.DefaultDeptId,
            Minzu = dto.Minzu,
            MVerifyPass = dto.MVerifyPass,
            Privilege = dto.Privilege,
            CardNo = dto.CardNo
        };

        const string sql = @"
INSERT INTO dbo.USERINFO
(
    BADGENUMBER, SSN, NAME, GENDER, TITLE, PAGER, BIRTHDAY, HIREDDAY, STREET,
    CITY, STATE, ZIP, OPHONE, FPHONE, VERIFICATIONMETHOD, DEFAULTDEPTID,
    ATT, INLATE, OUTEARLY, OVERTIME, SEP, HOLIDAY, MINZU, LUNCHDURATION,
    MVerifyPass, PHOTO, Notes, privilege, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, MinAutoSchInterval, RegisterOT, InheritDeptRule, EMPRIVILEGE,
    CardNo, FaceGroup, AccGroup, UseAccGroupTZ, VerifyCode, Expires,
    ValidCount, ValidTimeBegin, ValidTimeEnd, TimeZone1, TimeZone2, TimeZone3, Pin1
)
VALUES
(
    @BadgeNumber, @Ssn, @Name, @Gender, @Title, @Pager, @Birthday, @HiredDay, @Street,
    NULL, NULL, NULL, @OPhone, NULL, NULL, @DefaultDeptId,
    1, 1, 1, 1, 1, 1, @Minzu, 1,
    @MVerifyPass, @Photo, NULL, @Privilege, 1, 1,
    1, 24, 1, 1, 1,
    @CardNo, 1, 1, 1, 0, 0,
    0, NULL, NULL, 1, 1, 1, NULL
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new
            {
                model.BadgeNumber,
                model.Ssn,
                model.Name,
                model.Gender,
                model.Title,
                model.Pager,
                model.Birthday,
                model.HiredDay,
                model.Street,
                model.OPhone,
                model.DefaultDeptId,
                model.Minzu,
                model.MVerifyPass,
                Photo = dto.Photo,
                model.Privilege,
                model.CardNo
            });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("No fue posible registrar el empleado de prueba.", ex);
        }
    }

    public async Task UpdateAsync(EmpleadoDTO dto)
    {
        var model = new EmpleadoFormViewModel
        {
            UserId = dto.UserId,
            BadgeNumber = dto.BadgeNumber,
            Ssn = dto.Ssn ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            Gender = dto.Gender ?? string.Empty,
            Title = dto.Title,
            Pager = dto.Pager,
            Birthday = dto.Birthday,
            HiredDay = dto.HiredDay,
            Street = dto.Street,
            OPhone = dto.OPhone,
            DefaultDeptId = dto.DefaultDeptId,
            Minzu = dto.Minzu,
            MVerifyPass = dto.MVerifyPass,
            Privilege = dto.Privilege,
            CardNo = dto.CardNo
        };

        var result = await UpdateAsync(model, dto.Photo, "AutomatedTest", Environment.MachineName);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }
    }

    public async Task LogAsync(string operatorName, string description)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, @Description);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            Operator = operatorName,
            MachineAlias = Environment.MachineName,
            Description = description
        });
    }

    public async Task<(bool Success, string? Reason)> DeleteAsync(int userId)
    {
        var dependency = await ValidateDeleteAsync(userId);
        if (dependency.HasDependency)
        {
            return (false, dependency.DependencyMessage);
        }

        var result = await DeleteAsync(userId, "AutomatedTest", Environment.MachineName);
        return (result.Success, result.Message);
    }

    public async Task<int> CreateDepartmentAsync(string deptName, int? supDeptId)
    {
        const string sql = @"
INSERT INTO dbo.DEPARTMENTS
(
    DEPTNAME, SUPDEPTID, InheritParentSch, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, InLate, OutEarly, InheritDeptRule, MinAutoSchInterval,
    RegisterOT, DefaultSchId, ATT, Holiday, OverTime
)
VALUES
(
    @DeptName, @SupDeptId, 1, 1, 1,
    1, 1, 1, 1, 24,
    1, 1, 1, 1, 1
);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { DeptName = deptName, SupDeptId = supDeptId ?? 0 });
    }

    private static string GetPrivilegeDescription(int? privilege) => privilege switch
    {
        -1 => "Inválido",
        0 => "Usuario",
        1 => "Enrolar",
        2 => "Administrador",
        3 => "Supervisor",
        _ => string.Empty
    };
}