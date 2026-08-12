namespace ControlAsistencia.Web.Models;

public class UsuarioLoginDTO
{
    public int UserId { get; set; }

    public string BadgeNumber { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Password { get; set; } = string.Empty;

    public short? SecurityFlags { get; set; }
}