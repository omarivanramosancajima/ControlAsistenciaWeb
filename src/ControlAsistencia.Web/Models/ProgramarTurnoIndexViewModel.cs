namespace ControlAsistencia.Web.Models;

public class ProgramarTurnoIndexViewModel
{
    public IReadOnlyList<TurnoDTO> Turnos { get; set; } = Array.Empty<TurnoDTO>();
    public int? SelectedTurnoId { get; set; }
    public TurnoAsignacionDetalleViewModel? SelectedTurno { get; set; }
}