namespace ControlAsistencia.Web.Models;

public class ReubicacionEmpleadoTransferRequest
{
    public List<int> UserIds { get; set; } = [];
    public int TargetDeptId { get; set; }
}
