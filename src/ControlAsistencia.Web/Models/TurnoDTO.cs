using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class TurnoDTO
{
    public int NUM_RUNID { get; set; }

    [Display(Name = "Nombre")]
    public string NAME { get; set; } = string.Empty;

    [Display(Name = "Fecha de Inicio")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }
    public DateTime? STARTDATE
    {
        get => StartDate;
        set => StartDate = value;
    }

    [Display(Name = "Fecha Fin")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
    public DateTime? ENDDATE
    {
        get => EndDate;
        set => EndDate = value;
    }

    [Display(Name = "Ciclos")]
    public int? CYLE { get; set; }

    [Display(Name = "Frecuencia")]
    public int? UNITS { get; set; }

    public int? OLDID { get; set; }
}