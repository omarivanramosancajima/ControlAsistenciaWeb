using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class TurnosEmpleadoAssignRequest
{
    [Required]
    public List<int> UserIds { get; set; } = [];

    [Required]
    public int? NumRunId { get; set; }

    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }
}