namespace ControlAsistencia.Web.Models;

/// <summary>
/// Tipo de marca legado representado de forma tipada.
/// Incluye el cierre explícito del día anterior (CHECKTYPE = 'L').
/// </summary>
public enum AttendanceMarkType
{
    Unknown = 0,
    CheckIn = 1,
    CheckOut = 2,
    PreviousDayClosure = 3,
    Other = 4
}