using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Models;

/// <summary>
/// [ASISTWEB][SEC.00][SEC.01]
/// Contexto de entrada del motor para UNA persona y todo el rango solicitado.
/// Contiene la información de la persona y la lista completa de días ya preparados.
/// El motor es responsable de iterar Days y decidir qué resultados conserva.
/// </summary>
public class AttendanceCalculationContext
{
    public AttendancePersonContext PersonContext { get; set; } = new();
    public DateOnly FechaDesde { get; set; }
    public DateOnly FechaHasta { get; set; }
    public IReadOnlyList<AttendanceCalculationDayContext> Days { get; set; } = Array.Empty<AttendanceCalculationDayContext>();
}
