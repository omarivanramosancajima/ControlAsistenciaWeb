using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceHolidayProvider
{
    Task<AttendanceHolidayInfo> GetHolidayAsync(DateOnly date);
}