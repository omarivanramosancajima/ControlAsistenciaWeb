using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface ITurnosEmpleadoRepository
{
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<bool> DepartmentExistsAsync(int deptId);
    Task<IReadOnlyList<TurnoEmpleadoEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(int deptId, bool includeSubDependencies);
    Task<bool> UserExistsAsync(int userId);
    Task<TurnoEmpleadoEmployeeItemViewModel?> GetEmployeeByIdAsync(int userId);
    Task<IReadOnlyList<TurnoEmpleadoAsignacionItemViewModel>> GetAsignacionesByUserAsync(int userId);
    Task<TurnoEmpleadoAsignacionItemViewModel?> GetAsignacionByKeyAsync(int userId, int numOfRunId, DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<TurnoDTO>> GetTurnosAsync();
    Task<bool> TurnoExistsAsync(int numRunId);
    Task<TurnosEmpleadoAssignResult> AssignTurnoAsync(TurnosEmpleadoAssignRequest request, string operatorName, string machineAlias);
    Task<OperationResult> DeleteAsignacionAsync(TurnosEmpleadoDeleteRequest request, string operatorName, string machineAlias);
}