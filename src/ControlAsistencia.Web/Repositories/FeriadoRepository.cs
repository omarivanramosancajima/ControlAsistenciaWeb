using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ControlAsistencia.Web.Repositories;

public class FeriadoRepository : IFeriadoRepository
{
    private readonly string _connectionString;

    public FeriadoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<(IReadOnlyList<FeriadoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize)
    {
        const string sql = @"
SELECT 
    HOLIDAYID AS HolidayId,
    HolidayName,
    STARTTIME AS StartTime
FROM dbo.HOLIDAYS WITH (NOLOCK)
ORDER BY StartTime DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.HOLIDAYS WITH (NOLOCK);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (pageNumber - 1) * pageSize,
                PageSize = pageSize
            });

            var items = (await multi.ReadAsync<FeriadoDTO>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();

            return (items, totalRecords);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Ocurrió un error SQL al obtener feriados: {ex.Message}", ex);
        }
    }

    public async Task<FeriadoDTO?> GetByIdAsync(int holidayId)
    {
        const string sql = @"
SELECT TOP (1)
    HOLIDAYID AS HolidayId,
    HolidayName,
    STARTTIME AS StartTime
FROM dbo.HOLIDAYS WITH (NOLOCK)
WHERE HOLIDAYID = @HolidayId;";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<FeriadoDTO>(sql, new { HolidayId = holidayId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Ocurrió un error SQL al obtener feriado: {ex.Message}", ex);
        }
    }

    public async Task<bool> ExistsHolidayNameAsync(string holidayName, int? excludeHolidayId = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.HOLIDAYS WITH (NOLOCK)
WHERE HolidayName = @HolidayName
  AND (@ExcludeHolidayId IS NULL OR HOLIDAYID <> @ExcludeHolidayId);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                HolidayName = holidayName,
                ExcludeHolidayId = excludeHolidayId
            });

            return count > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Ocurrió un error SQL al validar nombre de feriado: {ex.Message}", ex);
        }
    }

    public async Task RegisterViewAuditAsync(string holidayName, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Visualiza Feriado: ' + @HolidayName);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                Operator = operatorName,
                MachineAlias = machineAlias,
                HolidayName = holidayName
            });
        }
        catch (SqlException)
        {
            // Loggear error sin exponer detalles
        }
    }

    public async Task<OperationResult> CreateAsync(FeriadoDTO model, string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.HOLIDAYS (
    HolidayName, 
    STARTTIME,
    HOLIDAYYEAR, HOLIDAYMONTH, HOLIDAYDAY, DURATION, HOLIDAYTYPE, XINBIE, MINZU, DeptID, timezone
) VALUES (
    @HolidayName, 
    @StartTime,
    NULL, NULL, 1, 1, NULL, NULL, NULL, 0, 0
);

DECLARE @HolidayNameLog VARCHAR(100) = @HolidayName;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Agrega Feriado: ' + @HolidayNameLog);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                model.HolidayName,
                model.StartTime,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Feriado registrado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"Error SQL: {ex.Message.Split('(')[0]}");
        }
    }

    public async Task<OperationResult> UpdateAsync(FeriadoDTO model, string operatorName, string machineAlias)
    {
        const string sql = @"
UPDATE dbo.HOLIDAYS
SET 
    HolidayName = @HolidayName,
    STARTTIME = @StartTime
WHERE HOLIDAYID = @HolidayId;

DECLARE @HolidayNameLog VARCHAR(100) = @HolidayName;
INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Edita Feriado: ' + @HolidayNameLog);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                model.HolidayId,
                model.HolidayName,
                model.StartTime,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Feriado actualizado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"Error SQL: {ex.Message.Split('(')[0]}");
        }
    }

    public async Task<OperationResult> DeleteAsync(int holidayId, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @HolidayName VARCHAR(100);
SELECT @HolidayName = HolidayName FROM dbo.HOLIDAYS WHERE HOLIDAYID = @HolidayId;

DELETE FROM dbo.HOLIDAYS WHERE HOLIDAYID = @HolidayId;

INSERT INTO dbo.SystemLog ([Operator], LogTime, MachineAlias, LogTag, LogDescr)
VALUES (@Operator, GETDATE(), @MachineAlias, 0, 'Elimina Feriado: ' + @HolidayName);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                HolidayId = holidayId,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Feriado eliminado correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail($"Error SQL: {ex.Message.Split('(')[0]}");
        }
    }
}