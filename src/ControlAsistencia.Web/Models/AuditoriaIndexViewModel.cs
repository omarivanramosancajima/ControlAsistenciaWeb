namespace ControlAsistencia.Web.Models;

public class AuditoriaIndexViewModel
{
    public IReadOnlyList<AuditoriaItemViewModel> Items { get; set; } =
        Array.Empty<AuditoriaItemViewModel>();

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public string? Search { get; set; }

    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
