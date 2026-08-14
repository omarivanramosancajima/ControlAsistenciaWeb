namespace ControlAsistencia.Web.Models;

public class NumRunDeilAsignacionDTO
{
    public int NUM_RUNID { get; set; }
    public DateTime? STARTTIME { get; set; }
    public DateTime? ENDTIME { get; set; }
    public int SDAYS { get; set; }
    public int? EDAYS { get; set; }
    public int? SCHCLASSID { get; set; }
    public int? OverTime { get; set; }
    public string? SchName { get; set; }
    public int? Color { get; set; }
}