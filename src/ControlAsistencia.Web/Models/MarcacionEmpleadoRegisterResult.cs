namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoRegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<MarcacionEmpleadoProgressItemViewModel> ProgressItems { get; set; } = [];
}