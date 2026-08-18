using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IAttendanceReportDemoRepository
{
    AttendanceReportIndexViewModel GetReport(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, string? estado, int page, int pageSize);
    IReadOnlyList<AttendanceReportPersonSummaryViewModel> GetPersons(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, string? estado);
}