namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoRegisterRequest
{
    public List<int> UserIds { get; set; } = [];
    public DateTime? CheckDate { get; set; }
    public string? CheckTime { get; set; }
    public string? Reason { get; set; }
}