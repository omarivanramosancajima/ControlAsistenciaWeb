namespace ControlAsistencia.Web.Models;

public class AttendanceReportPersonSummaryViewModel
{
    public int Codigo { get; set; }
    public string Dni { get; set; } = string.Empty;
    public string Personal { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string HorarioCodigo { get; set; } = string.Empty;
    public string HorarioRango { get; set; } = string.Empty;
    public string DiasAsistencia { get; set; } = string.Empty;
    public string DiasFalta { get; set; } = string.Empty;
    public string HorasEfectivas { get; set; } = string.Empty;
    public string HorasPermiso { get; set; } = string.Empty;
    public string Tardanza { get; set; } = string.Empty;
    public string SalidaTemprana { get; set; } = string.Empty;
    public string HorasExtras { get; set; } = string.Empty;
    public string DiasJustificados { get; set; } = string.Empty;
    public string DiasConTurno { get; set; } = string.Empty;
    public string DiasSinTurno { get; set; } = string.Empty;
    public string FeriadosConTurno { get; set; } = string.Empty;
    public string FeriadosSinTurno { get; set; } = string.Empty;
    public string HorasJustificadas { get; set; } = string.Empty;
    public IReadOnlyList<AttendanceReportRowViewModel> Rows { get; set; } = Array.Empty<AttendanceReportRowViewModel>();
}