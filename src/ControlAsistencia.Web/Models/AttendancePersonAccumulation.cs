namespace ControlAsistencia.Web.Models;

/// <summary>
/// [ASISTWEB][SEC.04]
/// Acumulados funcionales generados por el motor por persona y rango procesado.
/// </summary>
public class AttendancePersonAccumulation
{
    public int DiasProgramadosConTurno { get; set; }
    public int DiasDeAsistencia { get; set; }
    public int DiasDeAsistenciaSinTurno { get; set; }
    public int DiasConFalta { get; set; }
    public TimeSpan HorasEfectivas { get; set; }
    public TimeSpan HorasDePermanencia { get; set; }
    public TimeSpan TardanzasDelDia { get; set; }
    public TimeSpan SalidasTempranoDelDia { get; set; }
    public TimeSpan HorasExtras { get; set; }
    public int FeriadosConTurno { get; set; }
    public int FeriadosSinTurno { get; set; }
    public int DiasJustificados { get; set; }
    public TimeSpan HorasJustificadas { get; set; }
}