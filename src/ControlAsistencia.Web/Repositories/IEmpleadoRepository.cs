using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IEmpleadoRepository
{
    Task<(IReadOnlyList<EmpleadoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize);
    Task<EmpleadoDTO?> GetByIdAsync(int userId);
    Task<bool> ExistsBadgeNumberAsync(string badgeNumber, int? excludeUserId = null);
    Task<bool> DepartmentExistsAsync(int deptId);
    Task RegisterViewAuditAsync(string employeeName, string operatorName, string machineAlias);
    Task<OperationResult> CreateAsync(EmpleadoFormViewModel model, byte[]? photoBytes, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAsync(EmpleadoFormViewModel model, byte[]? photoBytes, string operatorName, string machineAlias);
    Task<DeleteDependencyResult> ValidateDeleteAsync(int userId);
    Task<OperationResult> DeleteAsync(int userId, string operatorName, string machineAlias);
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<OperationResult> CreateDepartmentAsync(string deptName, int supDeptId);
}