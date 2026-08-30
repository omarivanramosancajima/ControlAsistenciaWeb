using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class ExcepcionRepository : IExcepcionRepository
{
    private readonly string _connectionString;
    private static readonly IReadOnlyDictionary<int, string> UnitLabels = new Dictionary<int, string>
    {
        [1] = "Hora",
        [2] = "Minuto",
        [3] = "Día"
    };

    private static readonly IReadOnlyDictionary<string, string> DependencyMessages = new Dictionary<string, string>
    {
        ["USER_SPEDAY"] = "permisos y/o justificaciones asignados"
    };

    public ExcepcionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<(IReadOnlyList<ExcepcionDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
SELECT LeaveId, LeaveName, MinUnit, Unit, ReportSymbol, Classify
FROM dbo.LeaveClass WITH (NOLOCK)
ORDER BY LeaveName ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.LeaveClass WITH (NOLOCK);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var multi = await connection.QueryMultipleAsync(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
            return ((await multi.ReadAsync<ExcepcionDTO>()).ToList(), await multi.ReadFirstAsync<int>());
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Ocurrió un error SQL al obtener excepciones: {ex.Message}", ex);
        }
    }

    public async Task<ExcepcionDTO?> GetByIdAsync(int leaveId)
    {
        const string sql = @"
SELECT TOP (1) LeaveId, LeaveName, MinUnit, Unit, ReportSymbol, Classify
FROM dbo.LeaveClass WITH (NOLOCK)
WHERE LeaveId = @LeaveId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ExcepcionDTO>(sql, new { LeaveId = leaveId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Ocurrió un error SQL al obtener la excepción: {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsByNameAsync(string leaveName, int? excludeId = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.LeaveClass WITH (NOLOCK)
WHERE LeaveName = @LeaveName
  AND (@ExcludeId IS NULL OR LeaveId <> @ExcludeId);";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { LeaveName = leaveName, ExcludeId = excludeId }) > 0;
    }

    public async Task<OperationResult> CreateAsync(ExcepcionDTO model, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.LeaveClass (LeaveName, MinUnit, Unit, RemaindProc, RemaindCount, ReportSymbol, Deduct, Color, Classify)
VALUES (@LeaveName, @MinUnit, @Unit, 1, 1, @ReportSymbol, 0, 0, @Classify);

DECLARE @NameLog VARCHAR(100) = @LeaveName;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Agrega Excepcion: ' + @NameLog);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.LeaveName,
                model.MinUnit,
                model.Unit,
                model.ReportSymbol,
                Classify = model.Classify ? 0 : 128,
                Operator = operatorName,
                MachineAlias = machineAlias
            });

            return OperationResult.Ok("Permiso / justificación registrado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"No fue posible registrar permiso / justificación (ERROR: {ex.Number}: {ex.Message})");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"No fue posible registrar permiso / justificación ({ex.Message})");
        }
    }

    public async Task<OperationResult> UpdateAsync(ExcepcionDTO model, string operatorName, string machineAlias)
    {
        const string sql = @"
UPDATE dbo.LeaveClass
SET LeaveName = @LeaveName,
    MinUnit = @MinUnit,
    Unit = @Unit,
    ReportSymbol = @ReportSymbol,
    Classify = @Classify
WHERE LeaveId = @LeaveId;

DECLARE @NameLog VARCHAR(100) = @LeaveName;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Edita Excepcion: ' + @NameLog);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.LeaveId,
                model.LeaveName,
                model.MinUnit,
                model.Unit,
                model.ReportSymbol,
                Classify = model.Classify ? 0 : 128,
                Operator = operatorName,
                MachineAlias = machineAlias
            });

            return OperationResult.Ok("Permiso / justificación actualizado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"No fue posible actualizar permiso / justificación (ERROR: {ex.Number}: {ex.Message})");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"No fue posible actualizar permiso / justificación ({ex.Message})");
        }
    }

    public async Task<DeleteDependencyResult> ValidateDeleteAsync(int leaveId)
    {
        const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.USER_SPEDAY WITH (NOLOCK) WHERE DATEID = @LeaveId) SELECT 'USER_SPEDAY'
ELSE SELECT NULL;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var dependency = await connection.ExecuteScalarAsync<string?>(sql, new { LeaveId = leaveId });
            if (string.IsNullOrWhiteSpace(dependency)) return new DeleteDependencyResult { HasDependency = false };
            return new DeleteDependencyResult { HasDependency = true, DependencyMessage = $"No se puede eliminar el permiso/justificación porque tiene {DependencyMessages[dependency]}." };
        }
        catch (SqlException ex)
        {
            return new DeleteDependencyResult { HasDependency = true, DependencyMessage = $"No fue posible validar dependencias del permiso/justificación (ERROR: {ex.Number}: {ex.Message})" };
        }
        catch (Exception ex)
        {
            return new DeleteDependencyResult { HasDependency = true, DependencyMessage = $"No fue posible validar dependencias del permiso/justificación ({ex.Message})" };
        }
    }

    public async Task<OperationResult> DeleteAsync(int leaveId, string operatorName, string machineAlias)
    {
        var dependency = await ValidateDeleteAsync(leaveId);
        if (dependency.HasDependency)
        {
            return OperationResult.Fail(dependency.DependencyMessage ?? "No se puede eliminar el permiso/justificación.");
        }

        const string sql = @"
DECLARE @Name VARCHAR(100);
SELECT @Name = LeaveName FROM dbo.LeaveClass WHERE LeaveId = @LeaveId;
DELETE FROM dbo.LeaveClass WHERE LeaveId = @LeaveId;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Excepcion: ' + ISNULL(@Name, ''));";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { LeaveId = leaveId, Operator = operatorName, MachineAlias = machineAlias });
            return OperationResult.Ok("Permiso / justificación eliminado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"No fue posible eliminar permiso / justificación (ERROR: {ex.Number}: {ex.Message})");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"No fue posible eliminar permiso / justificación ({ex.Message})");
        }
    }

    public async Task RegisterViewAuditAsync(string leaveName, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Visualiza Excepcion: ' + @LeaveName);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { Operator = operatorName, MachineAlias = machineAlias, LeaveName = leaveName });
        }
        catch (SqlException)
        {
        }
    }
}