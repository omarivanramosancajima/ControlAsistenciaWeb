using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceReportService
{
    Task<AttendanceReportIndexViewModel> GetReportAsync(AttendanceReportRequest request);
    Task<IReadOnlyList<AttendanceReportPersonSummaryViewModel>> GetPersonsAsync(AttendanceReportRequest request);
}