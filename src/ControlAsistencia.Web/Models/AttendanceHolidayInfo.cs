namespace ControlAsistencia.Web.Models;

/// <summary>
/// Resultado de la consulta de feriado para una fecha concreta.
/// </summary>
public class AttendanceHolidayInfo
{
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
}