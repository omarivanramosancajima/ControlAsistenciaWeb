using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IAttendanceReportRepository
{
    Task<IReadOnlyList<AttendanceReportFilterPerson>> GetFilterPersonsAsync(string? personName, string? areaName);
    Task<IReadOnlyList<string>> GetAvailableAreasAsync();
    Task<IReadOnlyList<AttendanceReportAreaViewModel>> GetAreaHierarchyAsync();
    Task<IReadOnlyList<AttendanceReportFilterPerson>> GetFilterPersonsByAreaAsync(string? personName, int areaDeptId);
    Task<AttendanceReportCompanyInfo?> GetCompanyInfoAsync();
}