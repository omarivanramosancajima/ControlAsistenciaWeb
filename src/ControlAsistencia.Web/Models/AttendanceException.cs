namespace ControlAsistencia.Web.Models;

/// <summary>
/// Combinación funcional de USER_SPEDAY y LeaveClass para transportar una excepción/justificación.
/// </summary>
public class AttendanceException
{
    public int PersonId { get; set; }
    public short LeaveId { get; set; }
    public string LeaveName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string? Reason { get; set; }
    public double MinUnit { get; set; }
    public short Unit { get; set; }
    public short Classify { get; set; }
    public string ReportSymbol { get; set; } = string.Empty;
    public double Deduct { get; set; }
    public int Color { get; set; }
}