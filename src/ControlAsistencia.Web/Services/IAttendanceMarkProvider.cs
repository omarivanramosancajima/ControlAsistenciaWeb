using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceMarkProvider
{
    Task<IReadOnlyList<AttendanceMark>> GetMarksAsync(int personId, DateOnly date);
}