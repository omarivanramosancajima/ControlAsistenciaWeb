using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class TurnoFormViewModel : IValidatableObject
{
    public int NUM_RUNID { get; set; }

    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    [StringLength(30, ErrorMessage = "El Nombre no debe superar 30 caracteres.")]
    [Display(Name = "Nombre")]
    public string NAME { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Fecha de Inicio es obligatorio.")]
    [Display(Name = "Fecha de Inicio")]
    [DataType(DataType.Date)]
    public DateTime? STARTDATE { get; set; } 

    [Required(ErrorMessage = "El campo Fecha Fin es obligatorio.")]
    [Display(Name = "Fecha Fin")]
    [DataType(DataType.Date)]
    public DateTime? ENDDATE { get; set; } 

    [Required(ErrorMessage = "El campo Frecuencia es obligatorio.")]
    [Range(0, 3, ErrorMessage = "La Frecuencia debe ser 0, 1, 2 o 3.")]
    [Display(Name = "Frecuencia")]
    public int? UNITS { get; set; } = 1;

    [Required(ErrorMessage = "El campo Ciclos es obligatorio.")]
    [Range(1, 90, ErrorMessage = "El campo Ciclos debe estar entre 1 y 90.")]
    [Display(Name = "Ciclos")]
    public int? CYLE { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var units = new[] { 0, 1, 2, 3 };
        if (UNITS.HasValue && !units.Contains(UNITS.Value))
        {
            yield return new ValidationResult("El Frecuencia seleccionado no es válido.", new[] { nameof(UNITS) });
        }


        //if (!DateTime.TryParseExact(STARTDATE, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
        //{
        //    yield return new ValidationResult("La Fecha de Inicio debe tener formato dd/MM/yyyy.", new[] { nameof(STARTDATE) });
        //}

        //if (!DateTime.TryParseExact(ENDDATE, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
        //{
        //    yield return new ValidationResult("La Fecha Fin debe tener formato dd/MM/yyyy.", new[] { nameof(ENDDATE) });
        //}
    }
}