using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public AuthRepository(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<UsuarioLoginDTO?> ValidarLoginAsync(string badgeNumber, string password)
    {
        const string sql = @"
SELECT TOP (1)
    USERID AS UserId,
    BADGENUMBER AS BadgeNumber,
    NAME AS Name,
    [PASSWORD] AS [Password],
    SECURITYFLAGS AS SecurityFlags
FROM dbo.USERINFO WITH (NOLOCK)
WHERE BADGENUMBER = @BadgeNumber
  AND [PASSWORD] = @Password; 
  AND ISNULL(SECURITYFLAGS,0)>=5  " ;

        try
        {
            await using var connection = new SqlConnection(_connectionString);

            return await connection.QueryFirstOrDefaultAsync<UsuarioLoginDTO>(sql, new
            {
                BadgeNumber = badgeNumber,
                Password = password
            });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error al validar el login en SQL Server.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al validar el login.", ex);
        }
    }
}