using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IJustificarEmpleadoRepository
{
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<bool> DepartmentExistsAsync(int deptId);
    Task<IReadOnlyList<JustificarEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(int deptId, bool includeSubDependencies);
    Task<bool> UserExistsAsync(int userId);
    Task<JustificarEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId);
    Task<IReadOnlyList<JustificarEmpleadoExcepcionDisponibleViewModel>> GetExcepcionesAsync();
    Task<bool> ExcepcionExistsAsync(int leaveId);
    Task<JustificarEmpleadoExcepcionDisponibleViewModel?> GetExcepcionByIdAsync(int leaveId);
    Task<IReadOnlyList<JustificarEmpleadoExcepcionAsignacionViewModel>> GetAsignacionesByUserAsync(int userId);
    Task<JustificarEmpleadoAssignResult> AssignExcepcionAsync(
        JustificarEmpleadoAssignRequest request,
        string operatorName,
        string machineAlias);
    Task<JustificarEmpleadoDeleteConfirmViewModel?> GetDeleteConfirmationAsync(
        int userId,
        IReadOnlyList<JustificarEmpleadoDeleteItemRequest> items);
    Task<OperationResult> DeleteExcepcionesAsync(
        JustificarEmpleadoDeleteRequest request,
        string operatorName,
        string machineAlias);
}
