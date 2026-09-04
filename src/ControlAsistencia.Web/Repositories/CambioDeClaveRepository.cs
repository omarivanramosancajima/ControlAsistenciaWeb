using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public interface ICambioDeClaveRepository
{
    Task<CambioDeClaveUsuario?> GetAuthenticatedUserAsync(int userId);

    Task<OperationResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        string operatorName,
        string machineAlias);
}

public sealed class CambioDeClaveUsuario
{
    public string BadgeNumber { get; set; } = string.Empty;

    public string? Name { get; set; }
}

public class CambioDeClaveRepository : ICambioDeClaveRepository
{
    private readonly string _connectionString;

    public CambioDeClaveRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<CambioDeClaveUsuario?> GetAuthenticatedUserAsync(int userId)
    {
        const string sql = @"
SELECT TOP (1)
    BADGENUMBER AS BadgeNumber,
    NAME AS Name
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            return await connection.QueryFirstOrDefaultAsync<CambioDeClaveUsuario>(
                sql,
                new { UserId = userId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error SQL al obtener los datos del usuario autenticado.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Ocurrió un error inesperado al obtener los datos del usuario autenticado.", ex);
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        string operatorName,
        string machineAlias)
    {
        const string sql = @"
DECLARE @CurrentPassword VARCHAR(50);
DECLARE @Name VARCHAR(24);

SELECT
    @CurrentPassword = [PASSWORD],
    @Name = NAME
FROM dbo.USERINFO
WHERE USERID = @UserId;

IF @CurrentPassword IS NULL
    THROW 52101, 'La clave actual es incorrecta.', 1;

IF @CurrentPassword <> @CurrentPasswordInput
    THROW 52101, 'La clave actual es incorrecta.', 1;

UPDATE dbo.USERINFO
SET [PASSWORD] = @NewPassword
WHERE USERID = @UserId
  AND [PASSWORD] = @CurrentPasswordInput;

IF @@ROWCOUNT <> 1
    THROW 52102, 'No fue posible guardar el cambio de clave.', 1;

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
    LEFT('Cambio de clave: ' + ISNULL(@Name, ''), 50)
);";

        await using var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(
                sql,
                new
                {
                    UserId = userId,
                    CurrentPasswordInput = currentPassword,
                    NewPassword = newPassword,
                    Operator = operatorName,
                    MachineAlias = machineAlias
                },
                transaction);

            await transaction.CommitAsync();

            return OperationResult.Ok("El cambio de clave se realizó correctamente.");
        }
        catch (SqlException ex) when (ex.Number is 52101 or 52102)
        {
            return OperationResult.Fail(ex.Message);
        }
        catch (SqlException)
        {
            return OperationResult.Fail("No fue posible guardar el cambio de clave.");
        }
        catch (Exception)
        {
            return OperationResult.Fail("No fue posible guardar el cambio de clave.");
        }
    }
}
