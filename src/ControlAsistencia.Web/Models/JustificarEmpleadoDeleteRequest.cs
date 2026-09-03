namespace ControlAsistencia.Web.Models;

public class JustificarEmpleadoDeleteRequest
{
    public int UserId { get; set; }
    public List<JustificarEmpleadoDeleteItemRequest> Items { get; set; } = [];
}
