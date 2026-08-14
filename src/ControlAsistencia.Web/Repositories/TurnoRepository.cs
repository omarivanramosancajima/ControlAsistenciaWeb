using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private readonly string _connectionString;
    private static readonly IReadOnlyDictionary<string, string> DependencyMessages = new Dictionary<string, string>
    {
        ["NUM_RUN_DEIL"] = "dias programados para este turno"
    };

    public TurnoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<(IReadOnlyList<TurnoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
SELECT NUM_RUNID, OLDID, NAME, STARTDATE, ENDDATE, CYLE, UNITS
FROM dbo.NUM_RUN WITH (NOLOCK)
ORDER BY NAME ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.NUM_RUN WITH (NOLOCK);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var multi = await connection.QueryMultipleAsync(sql, new { Offset = (pageNumber - 1) * pageSize, PageSize = pageSize });
            return ((await multi.ReadAsync<TurnoDTO>()).ToList(), await multi.ReadFirstAsync<int>());
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener el listado de turnos.", ex); }
        catch (Exception ex) { throw new InvalidOperationException($"Ocurrió un error inesperado al obtener el listado de turnos.({ex.Message})", ex); }
    }

    public async Task<IReadOnlyList<TurnoDTO>> GetAllForScheduleAssignmentAsync()
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
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener los turnos para programación.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener los turnos para programación.", ex); }
    }

    public async Task<TurnoDTO?> GetByIdAsync(int numRunId)
    {
        const string sql = @"SELECT TOP (1) NUM_RUNID, OLDID, NAME, STARTDATE, ENDDATE, CYLE, UNITS FROM dbo.NUM_RUN WITH (NOLOCK) WHERE NUM_RUNID = @NumRunId;";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<TurnoDTO>(sql, new { NumRunId = numRunId });
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener el turno.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener el turno.", ex); }
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        const string sql = @"SELECT COUNT(1) FROM dbo.NUM_RUN WITH (NOLOCK) WHERE NAME = @Name AND (@ExcludeId IS NULL OR NUM_RUNID <> @ExcludeId);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(sql, new { Name = name, ExcludeId = excludeId }) > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al validar duplicidad de turnos.", ex); }
    }

    public async Task<IReadOnlyList<HorarioDTO>> GetHorariosForScheduleAssignmentAsync()
    {
        const string sql = @"
SELECT SCHCLASSID AS SchClassid,
       SCHNAME AS SchName,
       STARTTIME AS StartTime,
       ENDTIME AS EndTime,
       COLOR AS Color
FROM dbo.SchClass WITH (NOLOCK)
ORDER BY SCHNAME ASC;";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<HorarioDTO>(sql);
            return items.ToList();
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener los horarios para programación.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener los horarios para programación.", ex); }
    }

    public async Task<IReadOnlyList<NumRunDeilAsignacionDTO>> GetAsignacionesPorTurnoAsync(int numRunId)
    {
        const string sql = @"
SELECT d.NUM_RUNID,
       d.STARTTIME,
       d.ENDTIME,
       d.SDAYS,
       d.EDAYS,
       d.SCHCLASSID,
       d.OverTime,
       s.SCHNAME AS SchName,
       s.COLOR AS Color
FROM dbo.NUM_RUN_DEIL d WITH (NOLOCK)
LEFT JOIN dbo.SchClass s WITH (NOLOCK) ON s.SCHCLASSID = d.SCHCLASSID
WHERE d.NUM_RUNID = @NumRunId
ORDER BY d.SDAYS ASC;";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var items = await connection.QueryAsync<NumRunDeilAsignacionDTO>(sql, new { NumRunId = numRunId });
            return items.ToList();
        }
        catch (SqlException ex) { throw new InvalidOperationException("Ocurrió un error SQL al obtener las asignaciones del turno.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Ocurrió un error inesperado al obtener las asignaciones del turno.", ex); }
    }

    public async Task<OperationResult> SaveScheduleAssignmentsAsync(GuardarProgramacionTurnoRequest request, string operatorName, string machineAlias)
    {
        const string turnoSql = @"SELECT TOP (1) NUM_RUNID, NAME, STARTDATE, ENDDATE, CYLE, UNITS FROM dbo.NUM_RUN WITH (NOLOCK) WHERE NUM_RUNID = @NumRunId;";
        const string horarioSql = @"
SELECT TOP (1) SCHCLASSID AS SchClassid,
       SCHNAME AS SchName,
       STARTTIME AS StartTime,
       ENDTIME AS EndTime,
       COLOR AS Color
FROM dbo.SchClass WITH (NOLOCK)
WHERE SCHCLASSID = @SchClassId;";
        const string deleteSql = @"DELETE FROM dbo.NUM_RUN_DEIL WHERE NUM_RUNID = @NumRunId AND SDAYS = @DayNumber;";
        const string insertSql = @"
INSERT INTO dbo.NUM_RUN_DEIL (NUM_RUNID, STARTTIME, ENDTIME, SDAYS, EDAYS, SCHCLASSID, OverTime)
VALUES (@NumRunId, @StartTime, @EndTime, @DayNumber, @DayNumber, @SchClassId, 0);";
        const string logSql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, @LogDescr);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var turno = await connection.QueryFirstOrDefaultAsync<TurnoDTO>(turnoSql, new { request.NumRunId }, transaction);
            if (turno is null)
            {
                await transaction.RollbackAsync();
                return OperationResult.Fail("El turno seleccionado no existe.");
            }

            var units = turno.UNITS ?? -1;
            var cyle = turno.CYLE ?? 0;
            var totalDays = TurnoCycleDayHelper.GetTotalDays(units, cyle);
            if (totalDays <= 0)
            {
                await transaction.RollbackAsync();
                return OperationResult.Fail("El turno seleccionado no tiene una configuración de frecuencia válida.");
            }

            var horario = await connection.QueryFirstOrDefaultAsync<HorarioDTO>(horarioSql, new { request.SchClassId }, transaction);
            if (horario is null)
            {
                await transaction.RollbackAsync();
                return OperationResult.Fail("El horario seleccionado no existe.");
            }

            var selectedDays = request.SelectedDays.Distinct().OrderBy(x => x).ToList();
            var unselectedDays = request.UnselectedDays.Distinct().OrderBy(x => x).ToList();
            var invalidDay = selectedDays.Concat(unselectedDays).FirstOrDefault(day => day < 1 || day > totalDays);
            if (invalidDay != 0)
            {
                await transaction.RollbackAsync();
                return OperationResult.Fail("Se recibió un día fuera del rango permitido para el turno seleccionado.");
            }

            foreach (var day in unselectedDays)
            {
                await connection.ExecuteAsync(deleteSql, new { NumRunId = request.NumRunId, DayNumber = day }, transaction);
            }

            foreach (var day in selectedDays)
            {
                await connection.ExecuteAsync(deleteSql, new { NumRunId = request.NumRunId, DayNumber = day }, transaction);
                await connection.ExecuteAsync(insertSql, new
                {
                    NumRunId = request.NumRunId,
                    StartTime = horario.StartTime,
                    EndTime = horario.EndTime,
                    DayNumber = day,
                    SchClassId = request.SchClassId
                }, transaction);
            }

            await connection.ExecuteAsync(logSql, new
            {
                Operator = operatorName,
                MachineAlias = machineAlias,
                LogDescr = $"Programa Turno: {turno.NAME}"
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Asignación de horarios guardada correctamente.");
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible guardar la asignación de horarios.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible guardar la asignación de horarios.");
        }
    }

    public async Task<OperationResult> CreateAsync(TurnoFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.NUM_RUN (OLDID, NAME, STARTDATE, ENDDATE, CYLE, UNITS)
VALUES (-1, @NAME, @STARTDATE, @ENDDATE, @CYLE, @UNITS);

DECLARE @NameLog VARCHAR(30) = @NAME;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Agrega Turno: ' + @NameLog);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.NAME,
                model.STARTDATE, //= DateTime.ParseExact(model.STARTDATE, "dd/MM/yyyy", null),
                model.ENDDATE,   //= DateTime.ParseExact(model.ENDDATE, "dd/MM/yyyy", null),
                CYLE = model.CYLE,
                UNITS = model.UNITS,
                Operator = operatorName,
                MachineAlias = machineAlias
            });
            return OperationResult.Ok("Turno registrado correctamente.");
        }
        catch (SqlException exs) { return OperationResult.Fail("No fue posible registrar el turno por un error de base de datos ("+exs.Message+")."); }
        catch (Exception ex) { return OperationResult.Fail("No fue posible registrar el turno en este momento ("+ex.Message+")."); }
    }

    public async Task<OperationResult> UpdateAsync(TurnoFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
UPDATE dbo.NUM_RUN
SET NAME = @NAME,
    STARTDATE = @STARTDATE,
    ENDDATE = @ENDDATE,
    CYLE = @CYLE,
    UNITS = @UNITS
WHERE NUM_RUNID = @NUM_RUNID;

DECLARE @NameLog VARCHAR(30) = @NAME;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Edita Turno: ' + @NameLog);";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                model.NUM_RUNID,
                model.NAME,
                model.STARTDATE, //= DateTime.ParseExact(model.STARTDATE, "dd/MM/yyyy", null),
                model.ENDDATE, //= DateTime.ParseExact(model.ENDDATE, "dd/MM/yyyy", null),
                CYLE = model.CYLE,
                UNITS = model.UNITS,
                Operator = operatorName,
                MachineAlias = machineAlias
            });
            return OperationResult.Ok("Turno actualizado correctamente.");
        }
        catch (SqlException exs) { return OperationResult.Fail("No fue posible actualizar el turno por un error de base de datos+ ("+exs.Message+")."); }
        catch (Exception ex) { return OperationResult.Fail("No fue posible actualizar el turno en este momento++ ("+ex.Message+")."); }
    }

    public async Task<DeleteDependencyResult> ValidateDeleteAsync(int numRunId)
    {
        const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.NUM_RUN_DEIL WITH (NOLOCK) WHERE NUM_RUNID = @NumRunId) SELECT 'NUM_RUN_DEIL'
ELSE SELECT NULL;";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var dependency = await connection.ExecuteScalarAsync<string?>(sql, new { NumRunId = numRunId });
            if (string.IsNullOrWhiteSpace(dependency)) return new DeleteDependencyResult { HasDependency = false };
            return new DeleteDependencyResult { HasDependency = true, DependencyMessage = $"No se puede eliminar el turno porque tiene {DependencyMessages[dependency]}." };
        }
        catch (SqlException) { return new DeleteDependencyResult { HasDependency = true, DependencyMessage = "No se puede eliminar el turno por un error de base de datos." }; }
        catch (Exception) { return new DeleteDependencyResult { HasDependency = true, DependencyMessage = "No se puede eliminar el turno en este momento." }; }
    }

    public async Task<OperationResult> DeleteAsync(int numRunId, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @Name VARCHAR(30);
SELECT @Name = NAME FROM dbo.NUM_RUN WHERE NUM_RUNID = @NumRunId;
DELETE FROM dbo.NUM_RUN WHERE NUM_RUNID = @NumRunId;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Turno: ' + ISNULL(@Name, ''));";
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { NumRunId = numRunId, Operator = operatorName, MachineAlias = machineAlias });
            return OperationResult.Ok("Turno eliminado correctamente.");
        }
        catch (SqlException) { return OperationResult.Fail("No fue posible eliminar el turno por un error de base de datos."); }
        catch (Exception) { return OperationResult.Fail("No fue posible eliminar el turno en este momento."); }
    }

    public async Task RegisterViewAuditAsync(string nameTurno, string operatorName, string machineAlias)
    {
        const string sql = @"INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr) VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Visualiza Turno: ' + @NameTurno);";
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Operator = operatorName, MachineAlias = machineAlias, NameTurno = nameTurno });
    }
}