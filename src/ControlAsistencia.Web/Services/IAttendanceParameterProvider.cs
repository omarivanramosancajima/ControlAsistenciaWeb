using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public interface IAttendanceParameterProvider
{
    Task<AttendanceCalculationParameters> GetParametersAsync();
}