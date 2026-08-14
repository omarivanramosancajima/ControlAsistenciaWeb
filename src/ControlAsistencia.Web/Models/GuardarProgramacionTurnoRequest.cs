using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class GuardarProgramacionTurnoRequest
{
    [Required]
    public int NumRunId { get; set; }

    [Required]
    public int SchClassId { get; set; }

    public List<int> SelectedDays { get; set; } = new();
    public List<int> UnselectedDays { get; set; } = new();
}