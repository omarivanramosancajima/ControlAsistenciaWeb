using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

/// <summary>
/// DATOS DEMO TEMPORALES - NO REPRESENTAN EL MOTOR REAL DE ASISTENCIA.
/// Reemplazar posteriormente por el servicio real de asistencia.
/// </summary>
public class AttendanceReportDemoRepository : IAttendanceReportDemoRepository
{
    private static readonly IReadOnlyList<AttendanceReportPersonSummaryViewModel> DemoPersons = BuildDemoPersons();

    public AttendanceReportIndexViewModel GetReport(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, string? estado, int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;
        var from = fechaDesde ?? new DateTime(2025, 3, 1);
        var to = fechaHasta ?? new DateTime(2025, 3, 31);

        var filteredPersons = FilterPersons(from, to, persona, area);
        var filteredRows = FilterRows(filteredPersons, estado)
            .OrderBy(r => r.Personal)
            .ThenBy(r => r.Fecha)
            .ToList();

        var pagedRows = filteredRows
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new AttendanceReportIndexViewModel
        {
            FechaDesde = from,
            FechaHasta = to,
            Persona = string.IsNullOrWhiteSpace(persona) ? null : persona,
            Area = string.IsNullOrWhiteSpace(area) ? null : area,
            Estado = string.IsNullOrWhiteSpace(estado) ? null : estado,
            PageNumber = normalizedPage,
            PageSize = normalizedPageSize,
            TotalRecords = filteredRows.Count,
            Rows = pagedRows,
            Persons = filteredPersons,
            PersonasDisponibles = DemoPersons.Select(x => x.Personal).Distinct().OrderBy(x => x).ToList(),
            AreasDisponibles = DemoPersons.Select(x => x.Area).Distinct().OrderBy(x => x).ToList(),
            EstadosDisponibles = new[] { "Falta", "Tardanza", "Salida temprana", "Horas extras", "Excepción" }
        };
    }

    public IReadOnlyList<AttendanceReportPersonSummaryViewModel> GetPersons(DateTime? fechaDesde, DateTime? fechaHasta, string? persona, string? area, string? estado)
    {
        var from = fechaDesde ?? new DateTime(2025, 3, 1);
        var to = fechaHasta ?? new DateTime(2025, 3, 31);
        var filtered = FilterPersons(from, to, persona, area);

        if (string.IsNullOrWhiteSpace(estado))
        {
            return filtered;
        }

        return filtered
            .Select(person => new AttendanceReportPersonSummaryViewModel
            {
                Codigo = person.Codigo,
                Dni = person.Dni,
                Personal = person.Personal,
                Area = person.Area,
                HorarioCodigo = person.HorarioCodigo,
                HorarioRango = person.HorarioRango,
                DiasAsistencia = person.DiasAsistencia,
                DiasFalta = person.DiasFalta,
                HorasEfectivas = person.HorasEfectivas,
                HorasPermiso = person.HorasPermiso,
                Tardanza = person.Tardanza,
                SalidaTemprana = person.SalidaTemprana,
                HorasExtras = person.HorasExtras,
                DiasJustificados = person.DiasJustificados,
                Rows = FilterRows(new[] { person }, estado).ToList()
            })
            .Where(x => x.Rows.Count > 0)
            .ToList();
    }

    private static IReadOnlyList<AttendanceReportPersonSummaryViewModel> FilterPersons(DateTime from, DateTime to, string? persona, string? area)
    {
        return DemoPersons
            .Where(p => string.IsNullOrWhiteSpace(persona) || p.Personal.Equals(persona, StringComparison.OrdinalIgnoreCase))
            .Where(p => string.IsNullOrWhiteSpace(area) || p.Area.Equals(area, StringComparison.OrdinalIgnoreCase))
            .Select(p => new AttendanceReportPersonSummaryViewModel
            {
                Codigo = p.Codigo,
                Dni = p.Dni,
                Personal = p.Personal,
                Area = p.Area,
                HorarioCodigo = p.HorarioCodigo,
                HorarioRango = p.HorarioRango,
                DiasAsistencia = p.DiasAsistencia,
                DiasFalta = p.DiasFalta,
                HorasEfectivas = p.HorasEfectivas,
                HorasPermiso = p.HorasPermiso,
                Tardanza = p.Tardanza,
                SalidaTemprana = p.SalidaTemprana,
                HorasExtras = p.HorasExtras,
                DiasJustificados = p.DiasJustificados,
                Rows = p.Rows.Where(r => r.Fecha.Date >= from.Date && r.Fecha.Date <= to.Date).OrderBy(r => r.Fecha).ToList()
            })
            .Where(p => p.Rows.Count > 0)
            .ToList();
    }

    private static IEnumerable<AttendanceReportRowViewModel> FilterRows(IEnumerable<AttendanceReportPersonSummaryViewModel> persons, string? estado)
    {
        var rows = persons.SelectMany(x => x.Rows);

        if (string.IsNullOrWhiteSpace(estado))
        {
            return rows;
        }

        return estado.Trim() switch
        {
            "Falta" => rows.Where(r => string.Equals(r.Falta, "Si", StringComparison.OrdinalIgnoreCase)),
            "Tardanza" => rows.Where(r => !string.IsNullOrWhiteSpace(r.TardanzaEntrada)),
            "Salida temprana" => rows.Where(r => !string.IsNullOrWhiteSpace(r.SalidaTemprana)),
            "Horas extras" => rows.Where(r => !string.IsNullOrWhiteSpace(r.HorasExtras)),
            "Excepción" => rows.Where(r => !string.IsNullOrWhiteSpace(r.Excepcion)),
            _ => rows
        };
    }

    private static IReadOnlyList<AttendanceReportPersonSummaryViewModel> BuildDemoPersons()
    {
        return new List<AttendanceReportPersonSummaryViewModel>
        {
            new()
            {
                Codigo = 2,
                Dni = "71238426",
                Personal = "YASSHIRA MASIAS",
                Area = "SISTEMAS",
                HorarioCodigo = "HS01",
                HorarioRango = "08:00 - 18:00",
                DiasAsistencia = "17",
                DiasFalta = "4",
                HorasEfectivas = "119:06:00",
                HorasPermiso = "0:00",
                Tardanza = "6:20",
                SalidaTemprana = "21:42",
                HorasExtras = string.Empty,
                DiasJustificados = "0",
                Rows = new List<AttendanceReportRowViewModel>
                {
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,3),"HS01","08:00 - 18:00","08:07","17:18","No","07:00","07:00","00:07","00:42","","","13:04 15:15"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,4),"HS01","08:00 - 18:00","08:25","13:12","No","04:47","04:47","00:25","04:48","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,10),"HS01","08:00 - 18:00","08:54","17:05","No","08:11","08:11","00:54","00:55","","","13:02 15:19"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,11),"HS01","08:00 - 18:00","08:11","17:05","No","06:57","06:57","00:11","00:55","","","13:08 15:04"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,12),"HS01","08:00 - 18:00","08:00","17:23","No","07:23","07:23","00:00","00:37","","","13:02 15:01"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,13),"HS01","08:00 - 18:00","","","Si","","","","","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,14),"HS01","08:00 - 18:00","","","Si","","","","","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,17),"HS01","08:00 - 18:00","08:12","17:07","No","08:55","08:55","00:12","00:53","","","15:09"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,18),"HS01","08:00 - 18:00","08:05","17:01","No","06:53","06:53","00:05","00:59","","","13:03 15:06"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,19),"HS01","08:00 - 18:00","08:05","17:16","No","09:10","09:10","00:05","00:44","","","13:14 15:16"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,20),"HS01","08:00 - 18:00","","","Si","","","","","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,21),"HS01","08:00 - 18:00","08:15","17:31","No","07:08","07:08","00:15","00:29","","","13:03 15:10"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,24),"HS01","08:00 - 18:00","08:17","17:18","No","07:01","07:01","00:17","00:42","","","13:14 15:14"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,25),"HS01","08:00 - 18:00","08:33","17:04","No","06:22","06:22","00:33","00:56","","","13:04 15:12"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,26),"HS01","08:00 - 18:00","08:39","17:00","No","06:12","06:12","00:39","01:00","","","13:02 15:11"),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,27),"HS01","08:00 - 18:00","08:43","13:08","No","04:25","04:25","00:43","04:52","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,28),"HS01","08:00 - 18:00","","","Si","","","","","","",""),
                    Row(2,"71238426","YASSHIRA MASIAS","SISTEMAS",new DateTime(2025,3,31),"HS01","08:00 - 18:00","08:05","17:25","No","07:14","07:14","00:05","00:35","","","13:06 15:12")
                }
            },
            new()
            {
                Codigo = 5,
                Dni = "12345678",
                Personal = "FABRICIO",
                Area = "ADMINISTRACION",
                HorarioCodigo = "HR03",
                HorarioRango = "09:00 - 18:00",
                DiasAsistencia = "11",
                DiasFalta = "7",
                HorasEfectivas = "84:03:00",
                HorasPermiso = "0:00",
                Tardanza = "6:54",
                SalidaTemprana = "3:04",
                HorasExtras = "1:20",
                DiasJustificados = "2",
                Rows = new List<AttendanceReportRowViewModel>
                {
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,4),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,7),"HR03","09:00 - 18:00","09:02","18:23","No","07:57","08:20","00:02","","","","13:16 15:58 18:18"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,8),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,14),"HR03","09:00 - 18:00","08:45","19:20","No","08:00","09:34","","","01:20","","13:06 14:36"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,15),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,16),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,17),"HR03","09:00 - 18:00","09:36","18:00","No","07:23","07:23","00:36","","","","13:30"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,18),"HR03","09:00 - 18:00","11:32","16:26","No","03:54","03:54","02:32","01:34","","","13:06"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,21),"HR03","09:00 - 18:00","08:23","17:40","No","07:40","08:16","","00:20","","","13:05 15:22"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,22),"HR03","09:00 - 18:00","","","No","00:00","00:00","","","","LIC. MEDICA",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,23),"HR03","09:00 - 18:00","","","No","00:00","00:00","","","","LIC. MEDICA",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,24),"HR03","09:00 - 18:00","08:20","18:05","No","08:00","08:44","","","","","17:52"),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,25),"HR03","09:00 - 18:00","08:11","17:35","No","07:35","08:23","","00:25","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,28),"HR03","09:00 - 18:00","08:30","17:15","No","07:15","07:44","","00:45","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,29),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,30),"HR03","09:00 - 18:00","","","Si","","","","","","",""),
                    Row(5,"12345678","FABRICIO","ADMINISTRACION",new DateTime(2025,3,31),"HR03","09:00 - 18:00","08:38","18:09","No","08:00","08:31","","","","","13:16")
                }
            }
        };
    }

    private static AttendanceReportRowViewModel Row(int codigo, string dni, string personal, string area, DateTime fecha, string horarioCodigo, string horarioRango, string entrada, string salida, string falta, string horasEfectivas, string horasPermiso, string tardanzaEntrada, string salidaTemprana, string horasExtras, string excepcion, string marcasIntermedias)
    {
        return new AttendanceReportRowViewModel
        {
            Codigo = codigo,
            Dni = dni,
            Personal = personal,
            Area = area,
            Fecha = fecha,
            HorarioCodigo = horarioCodigo,
            HorarioRango = horarioRango,
            Entrada = entrada,
            Salida = salida,
            Falta = falta,
            HorasEfectivas = horasEfectivas,
            HorasPermiso = horasPermiso,
            TardanzaEntrada = tardanzaEntrada,
            SalidaTemprana = salidaTemprana,
            HorasExtras = horasExtras,
            Excepcion = excepcion,
            MarcasIntermedias = marcasIntermedias
        };
    }
}