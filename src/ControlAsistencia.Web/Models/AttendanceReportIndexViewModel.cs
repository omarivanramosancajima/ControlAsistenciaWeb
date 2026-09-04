namespace ControlAsistencia.Web.Models;

public class AttendanceReportIndexViewModel
{
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string? ValidationMessage { get; set; }
    public string? Persona { get; set; }
    public string? Area { get; set; }
    public int? AreaDeptId { get; set; }
    public string? Estado { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
    public IReadOnlyList<AttendanceReportRowViewModel> Rows { get; set; } = Array.Empty<AttendanceReportRowViewModel>();
    public IReadOnlyList<AttendanceReportPersonSummaryViewModel> Persons { get; set; } = Array.Empty<AttendanceReportPersonSummaryViewModel>();
    public IReadOnlyList<string> PersonasDisponibles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AreasDisponibles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<AttendanceReportAreaViewModel> AreasJerarquia { get; set; } = Array.Empty<AttendanceReportAreaViewModel>();
    public IReadOnlyList<string> EstadosDisponibles { get; set; } = Array.Empty<string>();
    public string CompanyTaxId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}