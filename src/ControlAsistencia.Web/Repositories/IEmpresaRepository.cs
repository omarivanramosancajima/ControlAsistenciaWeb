using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IEmpresaRepository
{
    Task<(IReadOnlyList<EmpresaItemViewModel> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber, int pageSize, string? search);

    Task<EmpresaFormViewModel?> GetByIdAsync(int companyId);
    Task<EmpresaDeleteViewModel?> GetDeleteByIdAsync(int companyId);

    Task<OperationResult> CreateAsync(
        EmpresaFormViewModel model, string operatorName, string machineAlias);

    Task<OperationResult> UpdateAsync(
        EmpresaFormViewModel model, string operatorName, string machineAlias);

    Task<OperationResult> DeleteAsync(
        int companyId, string operatorName, string machineAlias);

    Task<bool> CanManageAsync(int userId);
    Task RegisterViewAuditAsync(string operatorName, string machineAlias);
}
