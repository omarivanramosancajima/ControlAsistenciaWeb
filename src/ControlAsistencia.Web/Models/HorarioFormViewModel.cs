using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class HorarioFormViewModel : IValidatableObject
{
    public int SchClassid { get; set; }

    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string SchName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Hora de Entrada es obligatorio.")]
    [Display(Name = "Hora de Entrada")]
    public string StartTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Hora de Salida es obligatorio.")]
    [Display(Name = "Hora de Salida")]
    public string EndTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Tolerancia Entrada es obligatorio.")]
    [Range(0, 1440, ErrorMessage = "La Tolerancia Entrada debe estar entre 0 y 1440.")]
    [Display(Name = "Tolerancia Entrada")]
    public int? LateMinutes { get; set; }

    [Required(ErrorMessage = "El campo Tolerancia Salida es obligatorio.")]
    [Range(0, 1440, ErrorMessage = "La Tolerancia Salida debe estar entre 0 y 1440.")]
    [Display(Name = "Tolerancia Salida")]
    public int? EarlyMinutes { get; set; }

    [Display(Name = "Color")]
    public int Color { get; set; } = 16715535;

    [Display(Name = "Debe Ctrl Entrada")]
    public bool CheckIn { get; set; } = true;

    [Display(Name = "Debe Ctrl Salida")]
    public bool CheckOut { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TimeOnly.TryParseExact(StartTime, "HH:mm", out _))
        {
            yield return new ValidationResult("La Hora de Entrada debe tener formato 24 horas HH:mm.", new[] { nameof(StartTime) });
        }

        if (!TimeOnly.TryParseExact(EndTime, "HH:mm", out _))
        {
            yield return new ValidationResult("La Hora de Salida debe tener formato 24 horas HH:mm.", new[] { nameof(EndTime) });
        }

        if (!string.IsNullOrWhiteSpace(SchName) && SchName.Length > 50)
        {
            yield return new ValidationResult("El Nombre no debe superar 50 caracteres.", new[] { nameof(SchName) });
        }
    }
}