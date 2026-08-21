namespace ControlAsistencia.Web.Models;

/// <summary>
/// Representa exclusivamente las inconsistencias aprobadas para un día.
/// </summary>
public enum AttendanceInconsistencyKind
{
    None = 0,
    MissingEntry = 1,
    MissingExit = 2,
    SingleMarkWithoutNextDayClosure = 3
}