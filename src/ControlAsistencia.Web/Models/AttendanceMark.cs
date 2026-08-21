namespace ControlAsistencia.Web.Models;

/// <summary>
/// Marca de asistencia proveniente de CHECKINOUT o CHECKEXACT.
/// </summary>
public class AttendanceMark
{
    public AttendanceMarkSource Source { get; set; }
    public int? RecordId { get; set; }
    public int PersonId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CheckType { get; set; }
    public AttendanceMarkType MarkType { get; set; }

    /// <summary>
    /// Identifica explícitamente CHECKTYPE = 'L', cierre del día anterior.
    /// </summary>
    public bool IsPreviousDayClosureMark { get; set; }

    public int? VerifyCode { get; set; }
    public string? SensorId { get; set; }
    public string? DeviceSerialNumber { get; set; }
    public string? MemoInfo { get; set; }
    public string? WorkCode { get; set; }
    public bool IsManual { get; set; }
    public bool? IsAdded { get; set; }
    public bool? IsModified { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsCounted { get; set; }
    public short? InCount { get; set; }
    public string? Note { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? OperationDate { get; set; }
}