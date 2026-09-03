namespace ControlAsistencia.Web.Repositories;

using ControlAsistencia.Web.Models;

public interface IHorarioRepository
{
    Task<(IReadOnlyList<HorarioDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize, string? search = null);
    Task<HorarioDTO?> GetByIdAsync(int schClassid);
    Task<bool> ExistsByNameAsync(string schName, int? excludeId = null);
    Task<OperationResult> CreateAsync(HorarioFormViewModel model, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAsync(HorarioFormViewModel model, string operatorName, string machineAlias);
    Task<DeleteDependencyResult> ValidateDeleteAsync(int schClassid);
    Task<OperationResult> DeleteAsync(int schClassid, string operatorName, string machineAlias);
    Task RegisterViewAuditAsync(string schName, string operatorName, string machineAlias);
}