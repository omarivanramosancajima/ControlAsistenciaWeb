using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceExceptionProvider
{
    Task<IReadOnlyList<AttendanceException>> GetExceptionsAsync(int personId, DateOnly date);
}