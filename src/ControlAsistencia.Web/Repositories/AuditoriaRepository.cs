using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public interface IAuditoriaRepository
{
    Task<bool> CanViewAuditAsync(int userId);
    Task<(IReadOnlyList<AuditoriaItemViewModel> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber, int pageSize, string? search);
    Task RegisterViewAuditAsync(string operatorName, string machineAlias);
}

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly string _connectionString;

    public AuditoriaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<bool> CanViewAuditAsync(int userId)
    {
        const string sql = @"
SELECT CAST(CASE WHEN SECURITYFLAGS IN (15, 7) THEN 1 ELSE 0 END AS bit)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<bool>(sql, new { UserId = userId });
    }

    public async Task<(IReadOnlyList<AuditoriaItemViewModel> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber, int pageSize, string? search)
    {
        const string sql = @"
SELECT
    ID,
    [Operator],
    LogTime,
    MachineAlias,
    LogDescr
FROM dbo.SystemLog WITH (NOLOCK)
WHERE
    @Search IS NULL
    OR [Operator] LIKE '%' + @Search + '%'
    OR LogDescr LIKE '%' + @Search + '%'
ORDER BY LogTime DESC, ID DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.SystemLog WITH (NOLOCK)
WHERE
    @Search IS NULL
    OR [Operator] LIKE '%' + @Search + '%'
    OR LogDescr LIKE '%' + @Search + '%';";

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            await using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (pageNumber - 1) * pageSize,
                PageSize = pageSize,
                Search = search
            });

            var items = (await multi.ReadAsync<AuditoriaItemViewModel>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();

            return (items, totalRecords);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener los registros de auditoría.", ex);
        }
    }

    public async Task RegisterViewAuditAsync(string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog
(
    [Operator],
    LogTime,
    MachineAlias,
    LogTag,
    LogDescr
)
VALUES
(
    LEFT(@Operator, 20),
    GETDATE(),
    LEFT(@MachineAlias, 20),
    0,
    LEFT('Visualiza Auditoría', 50)
);";

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.ExecuteAsync(sql, new
            {
                Operator = operatorName,
                MachineAlias = machineAlias
            });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al registrar la auditoría de la consulta.", ex);
        }
    }
}
