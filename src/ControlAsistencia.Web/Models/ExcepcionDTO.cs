using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class ExcepcionDTO : IValidatableObject
{
    public int LeaveId { get; set; }

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El Nombre no debe superar 100 caracteres.")]
    public string LeaveName { get; set; } = string.Empty;

    [Display(Name = "Cantidad Equiv.")]
    [Required(ErrorMessage = "El campo Cantidad Equiv. es obligatorio.")]
    [Range(typeof(decimal), "0.10", "60.00", ErrorMessage = "La Cantidad Equiv. debe estar entre 0.10 y 60.00.")]
    public decimal MinUnit { get; set; } = 1.00m;

    [Display(Name = "Unidad de Medida")]
    [Range(1, 3, ErrorMessage = "La Unidad de Medida debe ser 1, 2 o 3.")]
    public int Unit { get; set; } = 1;

    [Display(Name = "Abreviatura")]
    [Required(ErrorMessage = "El campo Abreviatura es obligatorio.")]
    [StringLength(10, ErrorMessage = "La Abreviatura no debe superar 10 caracteres.")]
    public string ReportSymbol { get; set; } = string.Empty;

    [Display(Name = "Normal")]
    public bool Classify { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinUnit < 0.10m || MinUnit > 60.00m)
        {
            yield return new ValidationResult("La Cantidad Equiv. debe estar entre 0.10 y 60.00.", new[] { nameof(MinUnit) });
        }

        if (Unit is < 1 or > 3)
        {
            yield return new ValidationResult("La Unidad de Medida debe ser 1 (Hora), 2 (Minuto) o 3 (Día).", new[] { nameof(Unit) });
        }
    }
}