using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceCalculationContextBuilder
{
    Task<AttendanceCalculationContext?> BuildAsync(int personId, DateTime from, DateTime to);
}
