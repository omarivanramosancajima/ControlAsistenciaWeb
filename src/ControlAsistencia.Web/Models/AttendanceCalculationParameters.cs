namespace ControlAsistencia.Web.Models;

/// <summary>
/// Parámetros funcionales del cálculo de asistencia transportados desde AttParam.
/// </summary>
public class AttendanceCalculationParameters
{
    public bool AllowAfterOT { get; set; }
    public bool AllowEarlyOT { get; set; }
    public int IntervalOfAfterOT { get; set; }
    public int IntervalOfAfterOTAlternate { get; set; }
    public int IntervalOfEarlyOT { get; set; }
    public int IntervalOfEarlyOTAlternate { get; set; }
    public bool LimitAfterMaxOT { get; set; }
    public int AfterMaxOT { get; set; }
    public bool LimitEarlyMaxOT { get; set; }
    public int EarlyMaxOT { get; set; }
    public int NoInAbsent { get; set; }
    public int MinsNoIn { get; set; }
    public int NoOutAbsent { get; set; }
    public int MinsNoLeave { get; set; }
    public bool EarlyAbsent { get; set; }
    public int MinsEarlyAbsent { get; set; }
    public bool LateAbsent { get; set; }
    public int MinsLateAbsent { get; set; }
    public bool ShowNoTurn { get; set; }
    public bool AllowNoTurnOT { get; set; }
    public int LimitNoTurnOT { get; set; }
    public bool ShowHoliday { get; set; }
    public bool AllowHolidayOT { get; set; }
    public int LimitHolidayOT { get; set; }
    public bool WeekenFullDayOT { get; set; }
    public int Weekends { get; set; }
}