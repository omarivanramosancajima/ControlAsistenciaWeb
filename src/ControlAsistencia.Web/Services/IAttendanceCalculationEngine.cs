using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceCalculationEngine
{
    AttendanceDayResult Calculate(AttendanceCalculationContext context);
}