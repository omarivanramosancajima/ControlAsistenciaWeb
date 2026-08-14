using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface ITurnoRepository
{
    Task<(IReadOnlyList<TurnoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize);
    Task<IReadOnlyList<TurnoDTO>> GetAllForScheduleAssignmentAsync();
    Task<TurnoDTO?> GetByIdAsync(int numRunId);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<IReadOnlyList<HorarioDTO>> GetHorariosForScheduleAssignmentAsync();
    Task<IReadOnlyList<NumRunDeilAsignacionDTO>> GetAsignacionesPorTurnoAsync(int numRunId);
    Task<OperationResult> SaveScheduleAssignmentsAsync(GuardarProgramacionTurnoRequest request, string operatorName, string machineAlias);
    Task<OperationResult> CreateAsync(TurnoFormViewModel model, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAsync(TurnoFormViewModel model, string operatorName, string machineAlias);
    Task<DeleteDependencyResult> ValidateDeleteAsync(int numRunId);
    Task<OperationResult> DeleteAsync(int numRunId, string operatorName, string machineAlias);
    Task RegisterViewAuditAsync(string name, string operatorName, string machineAlias);
}