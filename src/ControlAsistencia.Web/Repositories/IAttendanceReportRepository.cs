using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IAttendanceReportRepository
{
    Task<IReadOnlyList<AttendanceReportFilterPerson>> GetFilterPersonsAsync(string? personName, string? areaName);
    Task<IReadOnlyList<string>> GetAvailableAreasAsync();
    Task<AttendanceReportCompanyInfo?> GetCompanyInfoAsync();
}