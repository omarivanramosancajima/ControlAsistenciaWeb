using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class TurnosEmpleadoDeleteRequest
{
    [Required]
    public int? UserId { get; set; }

    [Required]
    public int? NumOfRunId { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }
}