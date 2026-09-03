using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IMarcacionEmpleadoRepository
{
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<bool> DepartmentExistsAsync(int deptId);
    Task<IReadOnlyList<MarcacionEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(int deptId, bool includeSubDependencies);
    Task<bool> UserExistsAsync(int userId);
    Task<MarcacionEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId);
    Task<(IReadOnlyList<MarcacionEmpleadoMarcacionItemViewModel> Items, bool HasNextPage)> GetMarcacionesByUserAsync(int userId, int pageNumber, int pageSize);
    Task<MarcacionEmpleadoRegisterResult> RegisterManualMarkAsync(MarcacionEmpleadoRegisterRequest request, string operatorName, string machineAlias);
    Task<MarcacionEmpleadoDeleteConfirmViewModel?> GetDeleteConfirmationAsync(int userId, IReadOnlyList<MarcacionEmpleadoDeleteItemRequest> items);
    Task<OperationResult> DeleteMarcacionesAsync(MarcacionEmpleadoDeleteRequest request, string operatorName, string machineAlias);
}