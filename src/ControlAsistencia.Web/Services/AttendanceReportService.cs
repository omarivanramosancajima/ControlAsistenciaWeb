using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;

namespace ControlAsistencia.Web.Services;

public class AttendanceReportService : IAttendanceReportService
{
    private static readonly string[] ReportStates = ["Falta", "Tardanza", "Salida temprana", "Horas extras", "Excepción"];

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
        var company = await _repository.GetCompanyInfoAsync();
        var persons = await BuildPersonsAsync(normalized);
        var filteredRows = FilterRows(persons, normalized.Estado)
            .OrderBy(r => r.Personal)
            .ThenBy(r => r.Fecha)
            .ToList();

        var pagedRows = filteredRows
            .Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .ToList();

        var availablePersons = await _repository.GetFilterPersonsAsync(null, null);
        var availableAreas = await _repository.GetAvailableAreasAsync();

        return new AttendanceReportIndexViewModel
        {
            FechaDesde = normalized.FechaDesde!.Value,
            FechaHasta = normalized.FechaHasta!.Value,
            Persona = normalized.Persona,
            Area = normalized.Area,
            Estado = normalized.Estado,
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalRecords = filteredRows.Count,
            Rows = pagedRows,
            Persons = persons,
            PersonasDisponibles = availablePersons.Select(static x => x.PersonName).Where(static x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static x => x).ToList(),
            AreasDisponibles = availableAreas,
            EstadosDisponibles = ReportStates,
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
            var rows = await BuildRowsAsync(person.PersonId, request.FechaDesde!.Value, request.FechaHasta!.Value);
            var stateRows = FilterRows(rows, request.Estado).ToList();
            if (stateRows.Count == 0)
            {
                continue;
            }

            result.Add(BuildPersonSummary(stateRows));
        }

        return result;
    }

    private async Task<IReadOnlyList<AttendanceReportRowViewModel>> BuildRowsAsync(int personId, DateTime from, DateTime to)
    {
        var rows = new List<AttendanceReportRowViewModel>();

        for (var current = DateOnly.FromDateTime(from.Date); current <= DateOnly.FromDateTime(to.Date); current = current.AddDays(1))
        {
            var context = await _contextBuilder.BuildAsync(personId, current);
            if (context is null)
            {
                continue;
            }

            var dayResult = _engine.Calculate(context);
            if (!ShouldIncludeResult(dayResult))
            {
                continue;
            }

            rows.Add(MapRow(dayResult));
        }

        return rows;
    }

    private static bool ShouldIncludeResult(AttendanceDayResult result)
    {
        if (result.Schedule?.HasSchedule == true)
        {
            return true;
        }

        if (result.EntryMark is not null || result.ExitMark is not null)
        {
            return true;
        }

        if (result.IntermediateMarks.Count > 0)
        {
            return true;
        }

        return result.Exception is not null;
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
            HorasPermiso = FormatDuration(result.PresenceDuration),
            TardanzaEntrada = FormatDuration(result.LateEntryDuration),
            SalidaTemprana = FormatDuration(result.EarlyExitDuration),
            HorasExtras = FormatDuration(result.OvertimeDuration),
            Excepcion = result.Exception?.LeaveName ?? string.Empty,
            MarcasIntermedias = string.Join(' ', result.IntermediateMarks.Select(static x => x.Timestamp.ToString("HH:mm"))),
            TieneTurno = result.Schedule?.HasSchedule == true,
            EsFinDeSemana = result.IsWeekend,
            EsFeriado = result.IsHoliday,
            EsFeriadoConTurno = result.IsHolidayWithSchedule,
            EsFeriadoSinTurno = result.IsHolidayWithoutSchedule,
            EsSinTurno = result.IsNoSchedule,
            EstaJustificado = result.Exception is not null,
            HorasJustificadas = FormatDuration(result.JustifiedDuration)
        };
    }

    private static AttendanceReportPersonSummaryViewModel BuildPersonSummary(IReadOnlyList<AttendanceReportRowViewModel> rows)
    {
        var first = rows[0];
        var asistencia = rows.Count(static x => !string.Equals(x.Falta, "Si", StringComparison.OrdinalIgnoreCase));
        var falta = rows.Count(static x => string.Equals(x.Falta, "Si", StringComparison.OrdinalIgnoreCase));

        var horasEfectivas = SumDurations(rows.Select(static x => x.HorasEfectivas));
        var horasPermiso = SumDurations(rows.Select(static x => x.HorasPermiso));
        var tardanza = SumDurations(rows.Select(static x => x.TardanzaEntrada));
        var salidaTemprana = SumDurations(rows.Select(static x => x.SalidaTemprana));
        var horasExtras = SumDurations(rows.Select(static x => x.HorasExtras));
        var diasJustificados = rows.Count(static x => !string.IsNullOrWhiteSpace(x.Excepcion));
        var diasConTurno = rows.Count(static x => x.TieneTurno);
        var diasSinTurno = rows.Count(static x => x.EsSinTurno);
        var feriadosConTurno = rows.Count(static x => x.EsFeriadoConTurno);
        var feriadosSinTurno = rows.Count(static x => x.EsFeriadoSinTurno);
        var horasJustificadas = SumDurations(rows.Select(static x => x.HorasJustificadas));

        return new AttendanceReportPersonSummaryViewModel
        {
            Codigo = first.Codigo,
            Dni = first.Dni,
            Personal = first.Personal,
            Area = first.Area,
            HorarioCodigo = first.HorarioCodigo,
            HorarioRango = first.HorarioRango,
            DiasAsistencia = asistencia.ToString(),
            DiasFalta = falta.ToString(),
            HorasEfectivas = FormatSummaryDuration(horasEfectivas),
            HorasPermiso = FormatSummaryDuration(horasPermiso),
            Tardanza = FormatSummaryDuration(tardanza),
            SalidaTemprana = FormatSummaryDuration(salidaTemprana),
            HorasExtras = FormatSummaryDuration(horasExtras),
            DiasJustificados = diasJustificados.ToString(),
            DiasConTurno = diasConTurno.ToString(),
            DiasSinTurno = diasSinTurno.ToString(),
            FeriadosConTurno = feriadosConTurno.ToString(),
            FeriadosSinTurno = feriadosSinTurno.ToString(),
            HorasJustificadas = FormatSummaryDuration(horasJustificadas),
            Rows = rows.OrderBy(static x => x.Fecha).ToList()
        };
    }

    private static IEnumerable<AttendanceReportRowViewModel> FilterRows(IEnumerable<AttendanceReportPersonSummaryViewModel> persons, string? estado)
        => FilterRows(persons.SelectMany(static x => x.Rows), estado);

    private static IEnumerable<AttendanceReportRowViewModel> FilterRows(IEnumerable<AttendanceReportRowViewModel> rows, string? estado)
    {
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

    private static AttendanceReportRequest NormalizeRequest(AttendanceReportRequest request)
    {
        var from = request.FechaDesde?.Date ?? DateTime.Today.AddDays(-30);
        var to = request.FechaHasta?.Date ?? DateTime.Today;

        if (to < from)
        {
            (from, to) = (to, from);
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
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveScheduleCode(AttendanceDayResult result)
    {
        return result.Schedule?.ScheduleName ?? string.Empty;
    }

    private static string ResolveScheduleDisplay(AttendanceDayResult result)
    {
        if (result.Schedule is null || !result.Schedule.HasSchedule)
        {
            if (result.IsHoliday)
            {
                return "FERIADO";
            }

            return result.IsWeekend ? "FIN DE SEMANA" : string.Empty;
        }

        var range = FormatScheduleRange(result.Schedule);
        if (result.IsWeekend)
        {
            return string.IsNullOrWhiteSpace(range) ? "(FDS)" : $"(FDS) {range}";
        }

        if (result.IsHoliday)
        {
            return string.IsNullOrWhiteSpace(range) ? "(FER)" : $"(FER) {range}";
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
    {
        return mark?.Timestamp.ToString("HH:mm") ?? string.Empty;
    }

    private static string FormatDuration(TimeSpan? value)
    {
        if (!value.HasValue || value.Value <= TimeSpan.Zero)
        {
            return string.Empty;
        }

        return value.Value.ToString(@"hh\:mm");
    }

    private static TimeSpan SumDurations(IEnumerable<string> values)
    {
        var total = TimeSpan.Zero;
        foreach (var value in values)
        {
            if (TimeSpan.TryParse(value, out var parsed))
            {
                total += parsed;
            }
        }

        return total;
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
}