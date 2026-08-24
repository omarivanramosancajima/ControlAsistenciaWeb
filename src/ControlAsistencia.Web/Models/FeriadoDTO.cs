using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlAsistencia.Web.Models;

public class FeriadoDTO
{
    public int HolidayId { get; set; }

    [Display(Name = "Nombre día Festivo")]
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(20, ErrorMessage = "El Nombre no debe superar 20 caracteres.")]
    public string HolidayName { get; set; } = string.Empty;

    [Display(Name = "Fecha de Feriado")]
    [Required(ErrorMessage = "La fecha es obligatoria")]
    [DataType(DataType.Date)]
    [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
    public DateTime StartTime { get; set; }
}