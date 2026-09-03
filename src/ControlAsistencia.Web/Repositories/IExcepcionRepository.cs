using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IExcepcionRepository
{
    Task<(IReadOnlyList<ExcepcionDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize, string? search = null);
    Task<ExcepcionDTO?> GetByIdAsync(int leaveId);
    Task<bool> ExistsByNameAsync(string leaveName, int? excludeId = null);
    Task<OperationResult> CreateAsync(ExcepcionDTO model, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAsync(ExcepcionDTO model, string operatorName, string machineAlias);
    Task<DeleteDependencyResult> ValidateDeleteAsync(int leaveId);
    Task<OperationResult> DeleteAsync(int leaveId, string operatorName, string machineAlias);
    Task RegisterViewAuditAsync(string leaveName, string operatorName, string machineAlias);
}