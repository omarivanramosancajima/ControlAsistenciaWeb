namespace ControlAsistencia.Web.Models;

public class ReubicacionEmpleadoTransferResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<ReubicacionEmpleadoProgressItemViewModel> Items { get; set; } = [];
}
