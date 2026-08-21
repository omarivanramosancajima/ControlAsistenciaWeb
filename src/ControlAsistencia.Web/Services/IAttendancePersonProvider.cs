using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendancePersonProvider
{
    Task<AttendancePersonInfo?> GetByPersonIdAsync(int personId);
}