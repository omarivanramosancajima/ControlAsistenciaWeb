namespace ControlAsistencia.Web.Models;

public class AuditoriaItemViewModel
{
    public int Id { get; set; }
    public string? Operator { get; set; }
    public DateTime? LogTime { get; set; }
    public string? MachineAlias { get; set; }
    public string? LogDescr { get; set; }
}
