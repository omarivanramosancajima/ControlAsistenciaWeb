namespace ControlAsistencia.Web.Models;

public class TurnoEmpleadoAsignacionItemViewModel
{
    public int UserId { get; set; }
    public int NumOfRunId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public short? IsNotOfRun { get; set; }
    public int? OrderRun { get; set; }
    public string TurnoName { get; set; } = string.Empty;
    public DateTime? TurnoStartDate { get; set; }
    public DateTime? TurnoEndDate { get; set; }
    public int? Cyle { get; set; }
    public int? Units { get; set; }
    public string FrequencyText => TurnoCycleDayHelper.GetFrequencyLabel(Units ?? -1);
}