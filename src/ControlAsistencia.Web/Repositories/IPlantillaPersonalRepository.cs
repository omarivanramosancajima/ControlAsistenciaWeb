using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IPlantillaPersonalRepository
{
    Task<IReadOnlyList<DepartmentDTO>> GetDepartmentsHierarchyAsync();
    Task<bool> DepartmentExistsAsync(int deptId);
    Task<IReadOnlyList<PlantillaPersonalEmployeeItemViewModel>> GetEmployeesByDepartmentAsync(
        int deptId,
        bool includeSubDependencies);
}
