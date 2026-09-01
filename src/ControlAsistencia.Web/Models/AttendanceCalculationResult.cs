namespace ControlAsistencia.Web.Models;

/// <summary>
/// Resultado completo del motor para UNA persona y el rango procesado.
/// Los días descartados por las reglas del motor no aparecen en Days.
/// </summary>
public class AttendanceCalculationResult
{
    public AttendancePersonContext PersonContext { get; set; } = new();
    public IReadOnlyList<AttendanceDayResult> Days { get; set; } = Array.Empty<AttendanceDayResult>();
    public AttendancePersonAccumulation Accumulation { get; set; } = new();
}
