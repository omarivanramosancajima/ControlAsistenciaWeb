namespace ControlAsistencia.Web.Models;

public class TurnoAsignacionDetalleViewModel
{
    public int NumRunId { get; set; }
    public string NombreTurno { get; set; } = string.Empty;
    public int Units { get; set; }
    public int Cyle { get; set; }
    public string FrecuenciaTexto { get; set; } = string.Empty;
    public IReadOnlyList<TurnoDiaViewModel> Dias { get; set; } = Array.Empty<TurnoDiaViewModel>();
}