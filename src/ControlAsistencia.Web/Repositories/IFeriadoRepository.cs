using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IFeriadoRepository
{
    Task<(IReadOnlyList<FeriadoDTO> Items, int TotalRecords)> GetPagedAsync(int pageNumber, int pageSize, string? search = null);
    Task<FeriadoDTO?> GetByIdAsync(int holidayId);
    Task<bool> ExistsHolidayNameAsync(string holidayName, int? excludeHolidayId = null);
    Task RegisterViewAuditAsync(string holidayName, string operatorName, string machineAlias);
    Task<OperationResult> CreateAsync(FeriadoDTO model, string operatorName, string machineAlias);
    Task<OperationResult> UpdateAsync(FeriadoDTO model, string operatorName, string machineAlias);
    Task<OperationResult> DeleteAsync(int holidayId, string operatorName, string machineAlias);
}