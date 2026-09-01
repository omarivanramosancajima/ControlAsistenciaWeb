using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceCalculationEngine
{
    AttendanceCalculationResult Calculate(AttendanceCalculationContext context);
    AttendanceDayResult CalculateDay(AttendanceCalculationDayContext context);
}
