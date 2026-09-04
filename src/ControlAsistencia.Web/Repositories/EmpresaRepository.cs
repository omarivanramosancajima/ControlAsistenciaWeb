using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly string _connectionString;

    public EmpresaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<bool> CanManageAsync(int userId)
    {
        const string sql = @"
SELECT CAST(CASE WHEN SECURITYFLAGS IN (15, 7) THEN 1 ELSE 0 END AS bit)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE USERID = @UserId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            return await connection.QueryFirstOrDefaultAsync<bool>(sql, new { UserId = userId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("No fue posible validar los permisos de Empresas.", ex);
        }
    }

    public async Task<(IReadOnlyList<EmpresaItemViewModel> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber, int pageSize, string? search)
    {
        const string sql = @"
SELECT
    C.COMPANYID AS CompanyId,
    C.SCIA_TAXID AS TaxId,
    C.SCIA_DESCRIP AS Descrip,
    C.SCIA_TELF AS Tel,
    C.SCIA_MOVIL AS Movil,
    C.SCIA_EMAIL AS Email,
    D.DEPTNAME AS DeptName,
    C.DEPTID AS DeptId
FROM dbo.COMPANY C WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = C.DEPTID
WHERE C.estado_row = 'A'
  AND (
      @Search IS NULL
      OR C.SCIA_TAXID LIKE '%' + @Search + '%'
      OR C.SCIA_DESCRIP LIKE '%' + @Search + '%'
      OR D.DEPTNAME LIKE '%' + @Search + '%'
  )
ORDER BY C.SCIA_TAXID ASC, C.COMPANYID ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.COMPANY C WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = C.DEPTID
WHERE C.estado_row = 'A'
  AND (
      @Search IS NULL
      OR C.SCIA_TAXID LIKE '%' + @Search + '%'
      OR C.SCIA_DESCRIP LIKE '%' + @Search + '%'
      OR D.DEPTNAME LIKE '%' + @Search + '%'
  );";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await using var multi = await connection.QueryMultipleAsync(sql, new
            {
                Offset = (Math.Max(1, pageNumber) - 1) * pageSize,
                PageSize = pageSize,
                Search = search
            });

            var items = (await multi.ReadAsync<EmpresaItemViewModel>()).ToList();
            var totalRecords = await multi.ReadFirstAsync<int>();
            return (items, totalRecords);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener las empresas.", ex);
        }
    }

    public async Task<EmpresaFormViewModel?> GetByIdAsync(int companyId)
    {
        const string sql = @"
SELECT TOP (1)
    C.COMPANYID AS CompanyId,
    C.SCIA_TAXID AS TaxId,
    C.SCIA_DESCRIP AS Descrip,
    C.SCIA_TELF AS Tel,
    C.SCIA_MOVIL AS Movil,
    C.SCIA_EMAIL AS Email,
    C.SCIA_DIRECC AS Direc,
    D.DEPTNAME AS DeptName
FROM dbo.COMPANY C WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = C.DEPTID
WHERE C.COMPANYID = @CompanyId
  AND C.estado_row = 'A';";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            return await connection.QueryFirstOrDefaultAsync<EmpresaFormViewModel>(
                sql, new { CompanyId = companyId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al consultar la empresa.", ex);
        }
    }

    public async Task<EmpresaDeleteViewModel?> GetDeleteByIdAsync(int companyId)
    {
        const string sql = @"
SELECT TOP (1)
    C.COMPANYID AS CompanyId,
    C.SCIA_TAXID AS TaxId,
    C.SCIA_DESCRIP AS Descrip,
    C.SCIA_TELF AS Tel,
    C.SCIA_MOVIL AS Movil,
    C.SCIA_EMAIL AS Email,
    C.SCIA_DIRECC AS Direc,
    C.DEPTID AS DeptId,
    D.DEPTNAME AS DeptName
FROM dbo.COMPANY C WITH (NOLOCK)
INNER JOIN dbo.DEPARTMENTS D WITH (NOLOCK)
    ON D.DEPTID = C.DEPTID
WHERE C.COMPANYID = @CompanyId
  AND C.estado_row = 'A';";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            return await connection.QueryFirstOrDefaultAsync<EmpresaDeleteViewModel>(
                sql, new { CompanyId = companyId });
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al consultar la empresa.", ex);
        }
    }

    public async Task<OperationResult> CreateAsync(
        EmpresaFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
IF EXISTS (
    SELECT 1
    FROM dbo.COMPANY WITH (NOLOCK)
    WHERE SCIA_TAXID = @TaxId
)
    THROW 53001, 'El RUC ya existe.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.DEPARTMENTS WITH (NOLOCK)
    WHERE SUPDEPTID = 0
      AND DEPTNAME = @DeptName
)
    THROW 53002, 'La abreviatura ya existe entre las áreas raíz.', 1;

INSERT INTO dbo.DEPARTMENTS
(
    DEPTNAME, SUPDEPTID,
    InheritParentSch, InheritDeptSch, InheritDeptSchClass,
    AutoSchPlan, InLate, OutEarly, InheritDeptRule,
    MinAutoSchInterval, RegisterOT, DefaultSchId,
    ATT, Holiday, OverTime
)
VALUES
(
    @DeptName, 0,
    NULL, NULL, NULL,
    NULL, NULL, NULL, NULL,
    NULL, NULL, 1,
    NULL, NULL, NULL
);

DECLARE @DeptId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.COMPANY
(
    SCIA_TAXID, SCIA_DESCRIP, SCIA_TELF, SCIA_MOVIL,
    SCIA_EMAIL, SCIA_DIRECC, DEPTID, estado_row
)
VALUES
(
    @TaxId, @Descrip, @Tel, @Movil,
    @Email, @Direc, @DeptId, 'A'
);

DECLARE @CompanyId INT = CONVERT(INT, SCOPE_IDENTITY());

INSERT INTO dbo.SystemLog
(
    [Operator], LogTime, MachineAlias, LogTag, LogDescr
)
VALUES
(
    LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0,
    LEFT('Agrega Empresa: ' + @Descrip, 50)
);

SELECT @CompanyId;";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteScalarAsync<int>(sql, new
            {
                TaxId = model.TaxId.Trim(),
                Descrip = model.Descrip.Trim(),
                Tel = NullIfEmpty(model.Tel),
                Movil = NullIfEmpty(model.Movil),
                Email = model.Email.Trim(),
                Direc = NullIfEmpty(model.Direc),
                DeptName = model.DeptName.Trim(),
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Empresa registrada correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail(GetSqlMessage(ex));
        }
    }

    public async Task<OperationResult> UpdateAsync(
        EmpresaFormViewModel model, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @DeptId INT;
DECLARE @OldDeptName VARCHAR(30);
DECLARE @CompanyExists BIT = 0;

SELECT
    @DeptId = C.DEPTID,
    @CompanyExists = 1
FROM dbo.COMPANY C
WHERE C.COMPANYID = @CompanyId
  AND C.estado_row = 'A';

IF @CompanyExists = 0
    THROW 53003, 'La empresa no existe o ya no está activa.', 1;

SELECT @OldDeptName = D.DEPTNAME
FROM dbo.DEPARTMENTS D
WHERE D.DEPTID = @DeptId
  AND D.SUPDEPTID = 0;

IF @OldDeptName IS NULL
    THROW 53004, 'No se encontró el nodo raíz de la empresa.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.DEPARTMENTS D WITH (NOLOCK)
    WHERE D.SUPDEPTID = 0
      AND D.DEPTNAME = @DeptName
      AND D.DEPTID <> @DeptId
)
    THROW 53005, 'La abreviatura ya existe entre las áreas raíz.', 1;

UPDATE dbo.COMPANY
SET
    SCIA_TELF = @Tel,
    SCIA_MOVIL = @Movil,
    SCIA_EMAIL = @Email,
    SCIA_DIRECC = @Direc
WHERE COMPANYID = @CompanyId
  AND estado_row = 'A';

IF @OldDeptName <> @DeptName
BEGIN
    UPDATE dbo.DEPARTMENTS
    SET DEPTNAME = @DeptName
    WHERE DEPTID = @DeptId
      AND SUPDEPTID = 0;
END;

INSERT INTO dbo.SystemLog
(
    [Operator], LogTime, MachineAlias, LogTag, LogDescr
)
VALUES
(
    LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0,
    LEFT('Edita Empresa: ' + @Descrip, 50)
);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var affected = await connection.ExecuteAsync(sql, new
            {
                CompanyId = model.CompanyId,
                Tel = NullIfEmpty(model.Tel),
                Movil = NullIfEmpty(model.Movil),
                Email = model.Email.Trim(),
                Direc = NullIfEmpty(model.Direc),
                DeptName = model.DeptName.Trim(),
                Descrip = model.Descrip.Trim(),
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            if (affected < 1)
            {
                await transaction.RollbackAsync();
                return OperationResult.Fail("No se actualizó la empresa.");
            }

            await transaction.CommitAsync();
            return OperationResult.Ok("Empresa actualizada correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail(GetSqlMessage(ex));
        }
    }

    public async Task<OperationResult> DeleteAsync(
        int companyId, string operatorName, string machineAlias)
    {
        const string sql = @"
DECLARE @DeptId INT;
DECLARE @Descrip VARCHAR(100);
DECLARE @EmployeeCount INT;

SELECT
    @DeptId = C.DEPTID,
    @Descrip = C.SCIA_DESCRIP
FROM dbo.COMPANY C
WHERE C.COMPANYID = @CompanyId
  AND C.estado_row = 'A';

IF @DeptId IS NULL
    THROW 53006, 'La empresa no existe o ya fue eliminada.', 1;

SELECT @EmployeeCount = COUNT(1)
FROM dbo.USERINFO WITH (NOLOCK)
WHERE DEFAULTDEPTID = @DeptId;

IF @EmployeeCount > 0
    THROW 53007, 'No se puede eliminar la empresa porque tiene empleados asignados a su área.', 1;

UPDATE dbo.COMPANY
SET estado_row = 'U'
WHERE COMPANYID = @CompanyId
  AND estado_row = 'A';

IF @@ROWCOUNT <> 1
    THROW 53008, 'No se pudo realizar la eliminación lógica de la empresa.', 1;

INSERT INTO dbo.SystemLog
(
    [Operator], LogTime, MachineAlias, LogTag, LogDescr
)
VALUES
(
    LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0,
    LEFT('Elimina Empresa: ' + @Descrip, 50)
);";

        await using var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(sql, new
            {
                CompanyId = companyId,
                Operator = operatorName,
                MachineAlias = machineAlias
            }, transaction);

            await transaction.CommitAsync();
            return OperationResult.Ok("Empresa eliminada correctamente.");
        }
        catch (SqlException ex)
        {
            return OperationResult.Fail(GetSqlMessage(ex));
        }
    }

    public async Task RegisterViewAuditAsync(string operatorName, string machineAlias)
    {
        const string sql = @"
INSERT INTO dbo.SystemLog
(
    [Operator], LogTime, MachineAlias, LogTag, LogDescr
)
VALUES
(
    LEFT(@Operator, 20), GETDATE(), LEFT(@MachineAlias, 20), 0,
    LEFT('Visualiza Empresas', 50)
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
                "No fue posible registrar la auditoría de consulta de Empresas.", ex);
        }
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetSqlMessage(SqlException ex) =>
        ex.Message.Split('(')[0].Trim();
}
