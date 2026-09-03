namespace ControlAsistencia.Web.Models;
public class MarcacionEmpleadoMarcacionItemViewModel
{
    public int UserId { get; set; }
    public DateTime CheckTime { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public int? VerifyCode { get; set; }
    public string? SensorId { get; set; }
    public string? MemoInfo { get; set; }
    public string? WorkCode { get; set; }
    public string? SerialNumber { get; set; }
    public int? UserExtFmt { get; set; }
    public short? IsAdd { get; set; }
    public string? Reason { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public bool CanDelete { get; set; }
    public string RecordType { get; set; } = "De Equipo";
}