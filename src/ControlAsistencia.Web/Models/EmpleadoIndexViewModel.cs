namespace ControlAsistencia.Web.Models;

public class EmpleadoIndexViewModel
{
    public IReadOnlyList<EmpleadoDTO> Empleados { get; set; } = Array.Empty<EmpleadoDTO>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}