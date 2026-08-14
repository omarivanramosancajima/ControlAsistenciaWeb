using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class HorarioDTO
{
    public int SchClassid { get; set; }

    public int SCHCLASSID
    {
        get => SchClassid;
        set => SchClassid = value;
    }

    [Display(Name = "Nombre")]
    public string SchName { get; set; } = string.Empty;

    public string SCHNAME
    {
        get => SchName;
        set => SchName = value;
    }

    [Display(Name = "Hora de Entrada")]
    public DateTime? StartTime { get; set; }

    public TimeSpan? STARTTIME
    {
        get => StartTime?.TimeOfDay;
        set => StartTime = value.HasValue
        ? DateTime.Today.Add(value.Value)
        : null;
    }

    [Display(Name = "Hora de Salida")]
    public DateTime? EndTime { get; set; }

    public TimeSpan? ENDTIME
    {
        get => EndTime?.TimeOfDay;
        set => EndTime = value.HasValue
        ? DateTime.Today.Add(value.Value)
        : null;
    }

    [Display(Name = "Tolerancia Entrada")]
    public int? LateMinutes { get; set; }

    public int? LATEMINUTES
    {
        get => LateMinutes;
        set => LateMinutes = value;
    }

    [Display(Name = "Tolerancia Salida")]
    public int? EarlyMinutes { get; set; }

    public int? EARLYMINUTES
    {
        get => EarlyMinutes;
        set => EarlyMinutes = value;
    }

    public int? Color { get; set; }
    public int? CHECKIN { get; set; }
    public int? CHECKOUT { get; set; }
}