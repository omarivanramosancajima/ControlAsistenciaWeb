using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class AccesoAlSistemaEditViewModel
{
    public int UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string? Ssn { get; set; }
    public string? Name { get; set; }
    public string? DepartmentName { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoBase64 { get; set; }
    public short SecurityFlags { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el tipo de acceso.")]
    public short NewSecurityFlags { get; set; }

    public string AccessDescription { get; set; } = string.Empty;
}
