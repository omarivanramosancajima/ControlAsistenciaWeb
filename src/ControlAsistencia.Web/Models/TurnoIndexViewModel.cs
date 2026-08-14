namespace ControlAsistencia.Web.Models;

public class TurnoIndexViewModel
{
    public IReadOnlyList<TurnoDTO> Turnos { get; set; } = Array.Empty<TurnoDTO>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}