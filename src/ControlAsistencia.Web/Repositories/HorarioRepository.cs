using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class HorarioRepository : IHorarioRepository
{
    private readonly string _connectionString;
    private static readonly IReadOnlyDictionary<string, string> DependencyMessages = new Dictionary<string, string>
    {
        ["USER_TEMP_SCH"] = "turnos extraordinarios registrados",
        ["NUM_RUN_DEIL"] = "turnos registrados"
    };

    public HorarioRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<(IReadOnlyList<HorarioDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
SELECT 
    SCHCLASSID AS SchClassid, 
    SCHNAME AS SchName, 
    STARTTIME AS StartTime, 
    ENDTIME AS EndTime,
    LATEMINUTES AS LateMinutes, 
    EARLYMINUTES AS EarlyMinutes, 
    COLOR AS Color, 
    CHECKIN AS CHECKIN, 
    CHECKOUT AS CHECKOUT
FROM dbo.SchClass WITH (NOLOCK)
ORDER BY SCHNAME ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.SchClass WITH (NOLOCK);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var multi = await connection.QueryMultipleAsync(sql, new 
            { 
                Offset = (pageNumber - 1) * pageSize, 
                PageSize = pageSize 
            });
            //return ((await multi.ReadAsync<HorarioDTO>()).ToList(), await multi.ReadFirstAsync<int>());
            var items = (await multi.ReadAsync<HorarioDTO>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();

            return (items, totalRecords);

        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener el listado de horarios.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener el listado de horarios.("+ex.Message+")", ex); }
    }

    public async Task<HorarioDTO?> GetByIdAsync(int schClassid)
    {
        const string sql = @"
SELECT TOP (1) SCHCLASSID AS SchClassid, SCHNAME AS SchName, STARTTIME AS StartTime, ENDTIME AS EndTime,
       LATEMINUTES AS LateMinutes, EARLYMINUTES AS EarlyMinutes, COLOR AS Color, CHECKIN AS CHECKIN, CHECKOUT AS CHECKOUT
FROM dbo.SchClass WITH (NOLOCK)
WHERE SCHCLASSID = @SchClassid;";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<HorarioDTO>(sql, new { SchClassid = schClassid });
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener el horario.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener el horario.", ex); }
    }

    public async Task<bool> ExistsByNameAsync(string schName, int? excludeId = null)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.SchClass WITH (NOLOCK)
WHERE SCHNAME = @SchName AND (@ExcludeId IS NULL OR SCHCLASSID <> @ExcludeId);";
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { SchName = schName, ExcludeId = excludeId }) > 0;
    }

    public async Task<OperationResult> CreateAsync(HorarioFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SchClass (SCHNAME, STARTTIME, ENDTIME, LATEMINUTES, EARLYMINUTES, COLOR, CHECKIN, CHECKOUT,
                          CheckInTime1, CheckInTime2, CheckOutTime1, CheckOutTime2, AutoBind, WorkDay, SensorID, WorkMins)
VALUES (@SchName, @StartTime, @EndTime, @LateMinutes, @EarlyMinutes, @Color, @CheckIn, @CheckOut,
        NULL, NULL, NULL, NULL, 1, 1, NULL, 0);

DECLARE @Name VARCHAR(50) = @SchName;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Agrega Horario: ' + @Name);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.SchName,
                StartTime = TimeOnly.ParseExact(model.StartTime, "HH:mm").ToTimeSpan(),
                EndTime = TimeOnly.ParseExact(model.EndTime, "HH:mm").ToTimeSpan(),
                model.LateMinutes,
                model.EarlyMinutes,
                model.Color,
                CheckIn = model.CheckIn ? 1 : 0,
                CheckOut = model.CheckOut ? 1 : 0,
                Operator = operatorName,
                MachineAlias = machineAlias
            });
            return OperationResult.Ok("Horario registrado correctamente.");
        }
        catch (SqlException) { return OperationResult.Fail("No fue posible registrar el horario por un error de base de datos."); }
        catch (Exception) { return OperationResult.Fail("No fue posible registrar el horario en este momento."); }
    }

    public async Task<OperationResult> UpdateAsync(HorarioFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
UPDATE dbo.SchClass
SET SCHNAME = @SchName,
    STARTTIME = @StartTime,
    ENDTIME = @EndTime,
    LATEMINUTES = @LateMinutes,
    EARLYMINUTES = @EarlyMinutes,
    COLOR = @Color,
    CHECKIN = @CheckIn,
    CHECKOUT = @CheckOut
WHERE SCHCLASSID = @SchClassid;
 
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Edita Horario: ' + @Name);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.SchClassid,
                model.SchName,
                StartTime = TimeOnly.ParseExact(model.StartTime, "HH:mm").ToTimeSpan(),
                EndTime = TimeOnly.ParseExact(model.EndTime, "HH:mm").ToTimeSpan(),
                model.LateMinutes,
                model.EarlyMinutes,
                model.Color,
                CheckIn = model.CheckIn ? 1 : 0,
                CheckOut = model.CheckOut ? 1 : 0,
                Name = model.SchName,
                Operator = operatorName,
                MachineAlias = machineAlias
            });
            return OperationResult.Ok("Horario actualizado correctamente.");
        }
        catch (SqlException) { return OperationResult.Fail("No fue posible actualizar el horario por un error de base de datos."); }
        catch (Exception) { return OperationResult.Fail("No fue posible actualizar el horario en este momento."); }
    }

    public async Task<DeleteDependencyResult> ValidateDeleteAsync(int schClassid)
    {
        const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.USER_TEMP_SCH WITH (NOLOCK) WHERE SCHCLASSID = @SchClassid) SELECT 'USER_TEMP_SCH'
ELSE IF EXISTS (SELECT 1 FROM dbo.NUM_RUN_DEIL WITH (NOLOCK) WHERE SCHCLASSID = @SchClassid) SELECT 'NUM_RUN_DEIL'
ELSE SELECT NULL;";
        await using var connection = new SqlConnection(_connectionString);
        var dependency = await connection.ExecuteScalarAsync<string?>(sql, new { SchClassid = schClassid });
        if (string.IsNullOrWhiteSpace(dependency)) return new DeleteDependencyResult { HasDependency = false };
        return new DeleteDependencyResult { HasDependency = true, DependencyMessage = $"No se puede eliminar el horario porque tiene {DependencyMessages[dependency]}." };
    }

    public async Task<OperationResult> DeleteAsync(int schClassid, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @Name VARCHAR(50);
SELECT @Name = SCHNAME FROM dbo.SchClass WHERE SCHCLASSID = @SchClassid;
DELETE FROM dbo.SchClass WHERE SCHCLASSID = @SchClassid;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Horario: ' + ISNULL(@Name, ''));";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { SchClassid = schClassid, Operator = operatorName, MachineAlias = machineAlias });
            return OperationResult.Ok("Horario eliminado correctamente.");
        }
        catch (SqlException) { return OperationResult.Fail("No fue posible eliminar el horario por un error de base de datos."); }
        catch (Exception) { return OperationResult.Fail("No fue posible eliminar el horario en este momento."); }
    }

    public async Task RegisterViewAuditAsync(string schName, string operatorName, string machineAlias)
    {
        const string sql = @"INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr) VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Visualiza Horario: ' + @SchName);";
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Operator = operatorName, MachineAlias = machineAlias, SchName = schName });
    }
}