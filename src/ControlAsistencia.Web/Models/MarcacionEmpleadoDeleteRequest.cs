namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoDeleteRequest
{
    public int UserId { get; set; }
    public List<MarcacionEmpleadoDeleteItemRequest> Items { get; set; } = [];
}