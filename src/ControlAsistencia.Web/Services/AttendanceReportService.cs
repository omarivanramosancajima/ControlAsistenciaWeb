using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;

namespace ControlAsistencia.Web.Services;

public class AttendanceReportService : IAttendanceReportService
{
    private const int MaxCalendarDays = 62;

    private readonly IAttendanceReportRepository _repository;
    private readonly IAttendanceCalculationContextBuilder _contextBuilder;
    private readonly IAttendanceCalculationEngine _engine;

    public AttendanceReportService(
        IAttendanceReportRepository repository,
        IAttendanceCalculationContextBuilder contextBuilder,
        IAttendanceCalculationEngine engine)
    {
        _repository = repository;
        _contextBuilder = contextBuilder;
        _engine = engine;
    }

    public async Task<AttendanceReportIndexViewModel> GetReportAsync(AttendanceReportRequest request)
    {
        var normalized = NormalizeRequest(request);
        var persons = await BuildPersonsAsync(normalized);
        var filteredRows = FilterRows(persons.SelectMany(static x => x.Rows), normalized.Estado)
            .OrderBy(static r => r.Personal)
            .ThenBy(static r => r.Fecha)
            .ToList();

        var pagedRows = filteredRows
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList();

        var model = await BuildFilterModelAsync(normalized);
        model.PageNumber = normalized.PageNumber;
        model.PageSize = normalized.PageSize;
        model.TotalRecords = filteredRows.Count;
        model.Rows = pagedRows;
        model.Persons = persons;
        return model;
    }

    public async Task<AttendanceReportIndexViewModel> BuildFilterModelAsync(AttendanceReportRequest request)
    {
        var availablePersons = await _repository.GetFilterPersonsAsync(null, null);
        var availableAreas = await _repository.GetAvailableAreasAsync();
        var availableStates = BuildAvailableStates();
        var company = await _repository.GetCompanyInfoAsync();

        return new AttendanceReportIndexViewModel
        {
            FechaDesde = request.FechaDesde?.Date ?? DateTime.Today.AddDays(-30),
            FechaHasta = request.FechaHasta?.Date ?? DateTime.Today,
            Persona = NormalizeText(request.Persona),
            Area = NormalizeText(request.Area),
            Estado = NormalizeText(request.Estado),
            PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber,
            PageSize = request.PageSize <= 0 ? 20 : request.PageSize,
            PersonasDisponibles = availablePersons
                .Select(static x => x.PersonName)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static x => x)
                .ToList(),
            AreasDisponibles = availableAreas,
            EstadosDisponibles = availableStates,
            CompanyTaxId = company?.TaxId ?? string.Empty,
            CompanyName = company?.CompanyName ?? string.Empty
        };
    }

    public async Task<IReadOnlyList<AttendanceReportPersonSummaryViewModel>> GetPersonsAsync(AttendanceReportRequest request)
    {
        var normalized = NormalizeRequest(request);
        return await BuildPersonsAsync(normalized);
    }

    private async Task<IReadOnlyList<AttendanceReportPersonSummaryViewModel>> BuildPersonsAsync(AttendanceReportRequest request)
    {
        var filterPersons = await _repository.GetFilterPersonsAsync(request.Persona, request.Area);
        var result = new List<AttendanceReportPersonSummaryViewModel>();

        foreach (var person in filterPersons)
        {
            var dayResults = await BuildDayResultsAsync(person.PersonId, request.FechaDesde!.Value, request.FechaHasta!.Value);
            var includedResults = dayResults.Where(ShouldIncludeResult).ToList();
            var filteredResults = FilterResults(includedResults, request.Estado).ToList();
            if (filteredResults.Count == 0)
            {
                continue;
            }

            result.Add(BuildPersonSummary(filteredResults));
        }

        return result
            .OrderBy(static x => x.Personal)
            .ThenBy(static x => x.Codigo)
            .ToList();
    }

    private async Task<IReadOnlyList<AttendanceDayResult>> BuildDayResultsAsync(int personId, DateTime from, DateTime to)
    {
        var results = new List<AttendanceDayResult>();

        for (var current = DateOnly.FromDateTime(from.Date); current <= DateOnly.FromDateTime(to.Date); current = current.AddDays(1))
        {
            var context = await _contextBuilder.BuildAsync(personId, current);
            if (context is null)
            {
                continue;
            }

            results.Add(_engine.Calculate(context));
        }

        return results;
    }

    private static bool ShouldIncludeResult(AttendanceDayResult result)
    {
        if (result.Schedule?.HasSchedule == true || result.HasScheduledAssignment)
        {
            return true;
        }

        if (result.IsNoSchedule || result.IsHoliday || result.IsWeekend || result.IsHolidayWithSchedule || result.IsHolidayWithoutSchedule)
        {
            return true;
        }

        if (result.EntryMark is not null || result.ExitMark is not null || result.IntermediateMarks.Count > 0)
        {
            return true;
        }

        if (result.Exception is not null || result.HasExceptions || !string.IsNullOrWhiteSpace(result.ExceptionDisplayText))
        {
            return true;
        }

        return result.IsAbsent;
    }

    private static AttendanceReportRowViewModel MapRow(AttendanceDayResult result)
    {
        return new AttendanceReportRowViewModel
        {
            Codigo = int.TryParse(result.PersonCode, out var code) ? code : 0,
            Dni = result.PersonDocumentNumber ?? string.Empty,
            Personal = result.PersonName ?? string.Empty,
            Area = result.DepartmentName ?? string.Empty,
            Fecha = result.Date.ToDateTime(TimeOnly.MinValue),
            HorarioCodigo = ResolveScheduleCode(result),
            HorarioRango = ResolveScheduleDisplay(result),
            Entrada = FormatMark(result.EntryMark),
            Salida = FormatMark(result.ExitMark),
            Falta = result.IsAbsent ? "Si" : "No",
            HorasEfectivas = FormatDuration(result.EffectiveWorkDuration),
            HorasPermiso = FormatDuration(result.JustifiedDuration),
            TardanzaEntrada = FormatDuration(result.LateEntryDuration),
            SalidaTemprana = FormatDuration(result.EarlyExitDuration),
            HorasExtras = FormatDuration(result.OvertimeDuration),
            Excepcion = !string.IsNullOrWhiteSpace(result.ExceptionDisplayText)
                ? result.ExceptionDisplayText
                : result.Exception?.LeaveName ?? string.Empty,
            MarcasIntermedias = string.Join(' ', result.IntermediateMarks.Select(static x => x.Timestamp.ToString("HH:mm"))),
            TieneTurno = result.Schedule?.HasSchedule == true || result.HasScheduledAssignment,
            EsFinDeSemana = result.IsWeekend,
            EsFeriado = result.IsHoliday,
            EsFeriadoConTurno = result.IsHolidayWithSchedule,
            EsFeriadoSinTurno = result.IsHolidayWithoutSchedule,
            EsSinTurno = result.IsNoSchedule,
            EstaJustificado = result.Exception is not null || result.HasExceptions || !string.IsNullOrWhiteSpace(result.ExceptionDisplayText),
            HorasJustificadas = FormatDuration(result.JustifiedDuration)
        };
    }

    private static AttendanceReportPersonSummaryViewModel BuildPersonSummary(IReadOnlyList<AttendanceDayResult> results)
    {
        var first = results[0];
        var accumulation = results[^1].Accumulation;
        var rows = results
            .Select(MapRow)
            .OrderBy(static x => x.Fecha)
            .ToList();

        return new AttendanceReportPersonSummaryViewModel
        {
            Codigo = int.TryParse(first.PersonCode, out var code) ? code : 0,
            Dni = first.PersonDocumentNumber ?? string.Empty,
            Personal = first.PersonName ?? string.Empty,
            Area = first.DepartmentName ?? string.Empty,
            HorarioCodigo = ResolveScheduleCode(first),
            HorarioRango = ResolveScheduleDisplay(first),
            DiasAsistencia = accumulation.DiasDeAsistencia.ToString(),
            DiasFalta = accumulation.DiasConFalta.ToString(),
            HorasEfectivas = FormatSummaryDuration(accumulation.HorasEfectivas),
            HorasPermiso = FormatSummaryDuration(accumulation.HorasDePermanencia),
            Tardanza = FormatSummaryDuration(accumulation.TardanzasDelDia),
            SalidaTemprana = FormatSummaryDuration(accumulation.SalidasTempranoDelDia),
            HorasExtras = FormatSummaryDuration(accumulation.HorasExtras),
            DiasJustificados = accumulation.DiasJustificados.ToString(),
            DiasConTurno = accumulation.DiasProgramadosConTurno.ToString(),
            DiasSinTurno = accumulation.DiasDeAsistenciaSinTurno.ToString(),
            FeriadosConTurno = accumulation.FeriadosConTurno.ToString(),
            FeriadosSinTurno = accumulation.FeriadosSinTurno.ToString(),
            HorasJustificadas = FormatSummaryDuration(accumulation.HorasJustificadas),
            Rows = rows
        };
    }

    private static IEnumerable<AttendanceDayResult> FilterResults(IEnumerable<AttendanceDayResult> results, string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return results;
        }

        return NormalizeEstado(estado) switch
        {
            "Excepción" => results.Where(r => r.Exception is not null || r.HasExceptions || !string.IsNullOrWhiteSpace(r.ExceptionDisplayText)),
            "Falta" => results.Where(static r => r.IsAbsent),
            "Feriado" => results.Where(static r => r.IsHoliday),
            "Feriado con turno" => results.Where(static r => r.IsHolidayWithSchedule),
            "Permisos/Justificaciones" => results.Where(static r => (r.JustifiedDuration.HasValue && r.JustifiedDuration.Value > TimeSpan.Zero)
                || r.Exception is not null
                || r.HasExceptions
                || !string.IsNullOrWhiteSpace(r.ExceptionDisplayText)),
            "Asistencia sin turno" => results.Where(static r => r.IsNoSchedule && (r.EntryMark is not null || r.ExitMark is not null || r.IntermediateMarks.Count > 0 || (r.EffectiveWorkDuration.HasValue && r.EffectiveWorkDuration.Value > TimeSpan.Zero))),
            "Feriado sin turno" => results.Where(static r => r.IsHolidayWithoutSchedule),
            "FDS" => results.Where(static r => r.IsWeekend),
            "Horas extras" => results.Where(static r => r.OvertimeDuration.HasValue && r.OvertimeDuration.Value > TimeSpan.Zero),
            "Salida temprana" => results.Where(static r => r.EarlyExitDuration.HasValue && r.EarlyExitDuration.Value > TimeSpan.Zero),
            "Sin turno" => results.Where(static r => r.IsNoSchedule),
            "Sin marca" => results.Where(static r => r.EntryMark is null && r.ExitMark is null && r.IntermediateMarks.Count == 0),
            "Tardanza" => results.Where(static r => r.LateEntryDuration.HasValue && r.LateEntryDuration.Value > TimeSpan.Zero),
            _ => results
        };
    }

    private static IEnumerable<AttendanceReportRowViewModel> FilterRows(IEnumerable<AttendanceReportRowViewModel> rows, string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return rows;
        }

        return NormalizeEstado(estado) switch
        {
            "Excepción" => rows.Where(r => !string.IsNullOrWhiteSpace(r.Excepcion)),
            "Falta" => rows.Where(r => string.Equals(r.Falta, "Si", StringComparison.OrdinalIgnoreCase)),
            "Feriado" => rows.Where(static r => r.EsFeriado),
            "Feriado con turno" => rows.Where(static r => r.EsFeriadoConTurno),
            "Feriado sin turno" => rows.Where(static r => r.EsFeriadoSinTurno),
            "FDS" => rows.Where(static r => r.EsFinDeSemana),
            "Horas extras" => rows.Where(r => !string.IsNullOrWhiteSpace(r.HorasExtras)),
            "Permisos/Justificaciones" => rows.Where(r => !string.IsNullOrWhiteSpace(r.HorasPermiso)
                || !string.IsNullOrWhiteSpace(r.HorasJustificadas)
                || !string.IsNullOrWhiteSpace(r.Excepcion)),
            "Asistencia sin turno" => rows.Where(static r => r.EsSinTurno
                && (!string.IsNullOrWhiteSpace(r.Entrada)
                    || !string.IsNullOrWhiteSpace(r.Salida)
                    || !string.IsNullOrWhiteSpace(r.MarcasIntermedias)
                    || !string.IsNullOrWhiteSpace(r.HorasEfectivas))),
            "Salida temprana" => rows.Where(r => !string.IsNullOrWhiteSpace(r.SalidaTemprana)),
            "Sin turno" => rows.Where(static r => r.EsSinTurno),
            "Sin marca" => rows.Where(static r => string.IsNullOrWhiteSpace(r.Entrada) && string.IsNullOrWhiteSpace(r.Salida) && string.IsNullOrWhiteSpace(r.MarcasIntermedias)),
            "Tardanza" => rows.Where(r => !string.IsNullOrWhiteSpace(r.TardanzaEntrada)),
            _ => rows
        };
    }

    private static string NormalizeEstado(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return string.Empty;
        }

        var normalized = estado.Trim();
        return normalized.ToUpperInvariant() switch
        {
            "FALTAS" => "Falta",
            "TARDANZAS" => "Tardanza",
            "SALIDAS TEMPRANAS" => "Salida temprana",
            "HORAS EXTRA" => "Horas extras",
            "PERMISOS/JUSTIFICACIONES" => "Permisos/Justificaciones",
            "ASISTENCIA SIN TURNO" => "Asistencia sin turno",
            "FERIADOS" => "Feriado",
            _ => normalized
        };
    }

    private static IReadOnlyList<string> BuildAvailableStates()
        => new[]
        {
            "FALTAS",
            "TARDANZAS",
            "SALIDAS TEMPRANAS",
            "HORAS EXTRA",
            "PERMISOS/JUSTIFICACIONES",
            "ASISTENCIA SIN TURNO",
            "FERIADOS",
            "Feriado con turno",
            "Feriado sin turno",
            "FDS",
            "Sin marca",
            "Sin turno",
            "Excepción"
        };

    private static AttendanceReportRequest NormalizeRequest(AttendanceReportRequest request)
    {
        var from = request.FechaDesde?.Date ?? DateTime.Today.AddDays(-30);
        var to = request.FechaHasta?.Date ?? DateTime.Today;

        if (to < from)
        {
            throw new ArgumentException("La Fecha Desde no puede ser mayor que la Fecha Hasta.");
        }

        if ((to - from).TotalDays > MaxCalendarDays - 1)
        {
            throw new ArgumentException("El rango de fechas no puede exceder 62 días calendario.");
        }

        return new AttendanceReportRequest
        {
            FechaDesde = from,
            FechaHasta = to,
            Persona = NormalizeText(request.Persona),
            Area = NormalizeText(request.Area),
            Estado = NormalizeText(request.Estado),
            PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber,
            PageSize = request.PageSize <= 0 ? 20 : request.PageSize
        };
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ResolveScheduleCode(AttendanceDayResult result)
        => result.Schedule?.ScheduleName ?? string.Empty;

    private static string ResolveScheduleDisplay(AttendanceDayResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ScheduleDisplayText))
        {
            return result.ScheduleDisplayText;
        }

        if (result.Schedule is null || !result.Schedule.HasSchedule)
        {
            if (result.IsHolidayWithoutSchedule)
            {
                return "FERIADO SIN TURNO";
            }

            if (result.IsHoliday)
            {
                return "FERIADO";
            }

            if (result.IsWeekend)
            {
                return "FDS";
            }

            return result.IsNoSchedule ? "SIN TURNO" : string.Empty;
        }

        var range = FormatScheduleRange(result.Schedule);
        if (result.IsHolidayWithSchedule)
        {
            return string.IsNullOrWhiteSpace(range) ? "(FER)" : $"(FER) {range}";
        }

        if (result.IsWeekend)
        {
            return string.IsNullOrWhiteSpace(range) ? "(FDS)" : $"(FDS) {range}";
        }

        return range;
    }

    private static string FormatScheduleRange(AttendanceSchedule schedule)
    {
        if (schedule.ScheduledStartTime is null || schedule.ScheduledEndTime is null)
        {
            return string.Empty;
        }

        return $"{schedule.ScheduledStartTime:HH\\:mm} - {schedule.ScheduledEndTime:HH\\:mm}";
    }

    private static string FormatMark(AttendanceMark? mark)
        => mark?.Timestamp.ToString("HH:mm") ?? string.Empty;

    private static string FormatDuration(TimeSpan? value)
    {
        if (!value.HasValue || value.Value <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        var totalHours = (int)value.Value.TotalHours;
        return $"{totalHours:00}:{value.Value.Minutes:00}";
    }

    private static string FormatSummaryDuration(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return "0:00";
        }

        var totalHours = (int)value.TotalHours;
        return $"{totalHours}:{value.Minutes:00}";
    }

    private static string? ResolveState(AttendanceReportRowViewModel row)
    {
        if (!string.IsNullOrWhiteSpace(row.Excepcion))
        {
            return "Excepción";
        }

        if (row.EsFeriadoConTurno)
        {
            return "Feriado con turno";
        }

        if (row.EsFeriadoSinTurno)
        {
            return "Feriado sin turno";
        }

        if (row.EsFeriado)
        {
            return "Feriado";
        }

        if (row.EsFinDeSemana)
        {
            return "FDS";
        }

        if (row.EsSinTurno)
        {
            return "Sin turno";
        }

        if (string.IsNullOrWhiteSpace(row.Entrada) && string.IsNullOrWhiteSpace(row.Salida) && string.IsNullOrWhiteSpace(row.MarcasIntermedias))
        {
            return "Sin marca";
        }

        if (!string.IsNullOrWhiteSpace(row.HorasExtras))
        {
            return "Horas extras";
        }

        if (!string.IsNullOrWhiteSpace(row.SalidaTemprana))
        {
            return "Salida temprana";
        }

        if (!string.IsNullOrWhiteSpace(row.TardanzaEntrada))
        {
            return "Tardanza";
        }

        if (string.Equals(row.Falta, "Si", StringComparison.OrdinalIgnoreCase))
        {
            return "Falta";
        }

        return null;
    }
}