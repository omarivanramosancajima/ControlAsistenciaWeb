using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IAreaRepository
{
    Task<IReadOnlyList<AreaItemViewModel>> GetHierarchyAsync();
    Task<AreaItemViewModel?> GetByIdAsync(int deptId);
    Task<AreaOperationResult> CreateAsync(int parentDeptId, string deptName);
    Task<AreaOperationResult> UpdateAsync(int deptId, string deptName);
    Task<AreaOperationResult> DeleteAsync(int deptId);
}
