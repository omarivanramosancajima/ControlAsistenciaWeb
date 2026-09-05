namespace ControlAsistencia.Web.Models;

public sealed class HomeDailyAttendanceStatsViewModel
{
    public int TotalPersonas { get; init; }
    public int Faltas { get; init; }
    public int Asistencias { get; init; }
    public int Tardanzas { get; init; }
    public int SinTardanza { get; init; }
}
