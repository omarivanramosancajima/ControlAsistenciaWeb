using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceScheduleProvider
{
    Task<AttendanceSchedule?> GetScheduleAsync(int personId, DateOnly date);
}