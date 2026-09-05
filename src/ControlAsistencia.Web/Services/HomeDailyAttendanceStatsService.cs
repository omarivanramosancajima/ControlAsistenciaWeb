using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;

namespace ControlAsistencia.Web.Services;

/// <summary>
/// Indicadores del día actual para el Home.
/// Reutiliza exactamente el motor de asistencia existente.
/// No modifica marcas ni datos de asistencia.
/// </summary>
public sealed class HomeDailyAttendanceStatsService : IHomeDailyAttendanceStatsService
{
    private readonly IAttendanceReportRepository _personRepository;
    private readonly IAttendanceCalculationContextBuilder _contextBuilder;
    private readonly IAttendanceCalculationEngine _engine;
    private readonly ILogger<HomeDailyAttendanceStatsService> _logger;

    public HomeDailyAttendanceStatsService(
        IAttendanceReportRepository personRepository,
        IAttendanceCalculationContextBuilder contextBuilder,
        IAttendanceCalculationEngine engine,
        ILogger<HomeDailyAttendanceStatsService> logger)
    {
        _personRepository = personRepository;
        _contextBuilder = contextBuilder;
        _engine = engine;
        _logger = logger;
    }

    public async Task<HomeDailyAttendanceStatsViewModel> GetTodayAsync(
        CancellationToken cancellationToken = default)
    {
        // [ASISTWEB][HOME][DIA-ACTUAL]
        // El cálculo se realiza exclusivamente con el motor de asistencia vigente.
        var today = DateTime.Today;
        var persons = await _personRepository.GetFilterPersonsAsync(null, null);

        var procesadas = 0;
        var faltas = 0;
        var tardanzas = 0;

        foreach (var person in persons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = await _contextBuilder.BuildAsync(person.PersonId, today, today);
            if (context is null)
            {
                continue;
            }

            var calculation = _engine.Calculate(context);
            var day = calculation.Days.FirstOrDefault(
                x => x.Date == DateOnly.FromDateTime(today));

            // El motor puede descartar un día según sus reglas vigentes.
            if (day is null)
            {
                continue;
            }

            procesadas++;

            if (day.IsAbsent)
            {
                faltas++;
            }

            if (day.LateEntryDuration.HasValue &&
                day.LateEntryDuration.Value > TimeSpan.Zero)
            {
                tardanzas++;
            }
        }

        var asistencias = Math.Max(procesadas - faltas, 0);
        var sinTardanza = Math.Max(procesadas - tardanzas, 0);

        _logger.LogDebug(
            "Indicadores Home del día {Fecha}: procesadas={Procesadas}, faltas={Faltas}, asistencias={Asistencias}, tardanzas={Tardanzas}.",
            today,
            procesadas,
            faltas,
            asistencias,
            tardanzas);

        return new HomeDailyAttendanceStatsViewModel
        {
            TotalPersonas = procesadas,
            Faltas = faltas,
            Asistencias = asistencias,
            Tardanzas = tardanzas,
            SinTardanza = sinTardanza
        };
    }
}
