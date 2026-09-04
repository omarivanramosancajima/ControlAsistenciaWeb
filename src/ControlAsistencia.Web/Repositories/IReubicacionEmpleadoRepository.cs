using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IReubicacionEmpleadoRepository
{
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<bool> DepartmentExistsAsync(int deptId);
    Task<IReadOnlyList<ReubicacionEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(
        int deptId,
        bool includeSubDependencies);
    Task<IReadOnlyList<ReubicacionEmpleadoEmployeeItemViewModel>> GetEmployeesByUserIdsAsync(
        IReadOnlyCollection<int> userIds);
    Task<ReubicacionEmpleadoProgressItemViewModel> TransferEmployeeAsync(
        int userId,
        int targetDeptId);
}
