using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class CambioDeClaveViewModel
{
    public string BadgeNumber { get; set; } = string.Empty;

    public string? Name { get; set; }

    [Required(ErrorMessage = "La clave actual es obligatoria.")]
    [MaxLength(50, ErrorMessage = "La clave actual no puede superar 50 caracteres.")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "El nuevo password es obligatorio.")]
    [MinLength(5, ErrorMessage = "La nueva clave debe tener mínimo 5 caracteres.")]
    [MaxLength(15, ErrorMessage = "La nueva clave debe tener máximo 15 caracteres.")]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "La nueva clave solo puede contener letras y números, sin espacios ni caracteres especiales.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Debe repetir el nuevo password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Las nuevas claves no coinciden.")]
    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }

    public string? SuccessMessage { get; set; }
}
