using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public class AttendanceCalculationContextBuilder : IAttendanceCalculationContextBuilder
{
    private readonly IAttendancePersonProvider _personProvider;
    private readonly IAttendanceScheduleProvider _scheduleProvider;
    private readonly IAttendanceMarkProvider _markProvider;
    private readonly IAttendanceParameterProvider _parameterProvider;
    private readonly IAttendanceHolidayProvider _holidayProvider;
    private readonly IAttendanceExceptionProvider _exceptionProvider;

    public AttendanceCalculationContextBuilder(
        IAttendancePersonProvider personProvider,
        IAttendanceScheduleProvider scheduleProvider,
        IAttendanceMarkProvider markProvider,
        IAttendanceParameterProvider parameterProvider,
        IAttendanceHolidayProvider holidayProvider,
        IAttendanceExceptionProvider exceptionProvider)
    {
        _personProvider = personProvider;
        _scheduleProvider = scheduleProvider;
        _markProvider = markProvider;
        _parameterProvider = parameterProvider;
        _holidayProvider = holidayProvider;
        _exceptionProvider = exceptionProvider;
    }

    /// <summary>
    /// Construye el contexto completo de UNA persona para TODO el rango solicitado.
    /// El bucle de días pertenece al contexto/orquestación de preparación; el motor decide
    /// cuáles resultados conserva y procesa todos los días recibidos.
    /// </summary>
    public async Task<AttendanceCalculationContext?> BuildAsync(int personId, DateTime from, DateTime to)
    {
        if (from.Date > to.Date)
        {
            throw new ArgumentException("FechaDesde no puede ser mayor que FechaHasta.");
        }

        var person = await _personProvider.GetByPersonIdAsync(personId);
        if (person is null)
        {
            return null;
        }

        var parameters = await _parameterProvider.GetParametersAsync();
        var days = new List<AttendanceCalculationDayContext>();

        for (var current = DateOnly.FromDateTime(from.Date);
             current <= DateOnly.FromDateTime(to.Date);
             current = current.AddDays(1))
        {
            days.Add(await BuildDayAsync(person, parameters, current));
        }

        return new AttendanceCalculationContext
        {
            PersonContext = new AttendancePersonContext
            {
                PersonId = person.PersonId,
                PersonCode = person.PersonCode,
                PersonDocumentNumber = person.PersonDocumentNumber,
                PersonName = person.PersonName,
                DepartmentId = person.DepartmentId,
                DepartmentName = person.DepartmentName,
                CompanyTaxId = person.CompanyTaxId,
                CompanyName = person.CompanyName,
                CompanyDepartmentId = person.CompanyDepartmentId,
                CompanyResolutionDiagnostic = person.CompanyResolutionDiagnostic
            },
            FechaDesde = DateOnly.FromDateTime(from.Date),
            FechaHasta = DateOnly.FromDateTime(to.Date),
            Days = days
        };
    }

    private async Task<AttendanceCalculationDayContext> BuildDayAsync(
        AttendancePersonInfo person,
        AttendanceCalculationParameters parameters,
        DateOnly date)
    {
        // [ASISTWEB][SEC.01.01][SEC.01.02][SEC.01.03][SEC.01.04]
        var schedule = await _scheduleProvider.GetScheduleAsync(person.PersonId, date);
        var marks = await _markProvider.GetMarksAsync(person.PersonId, date);
        var nextDayMarks = await _markProvider.GetMarksAsync(person.PersonId, date.AddDays(1));
        var holiday = await _holidayProvider.GetHolidayAsync(date);
        var exceptions = await _exceptionProvider.GetExceptionsAsync(person.PersonId, date);

        return new AttendanceCalculationDayContext
        {
            PersonId = person.PersonId,
            PersonCode = person.PersonCode,
            PersonDocumentNumber = person.PersonDocumentNumber,
            PersonName = person.PersonName,
            DepartmentId = person.DepartmentId,
            DepartmentName = person.DepartmentName,
            CompanyTaxId = person.CompanyTaxId,
            CompanyName = person.CompanyName,
            CompanyResolutionDiagnostic = person.CompanyResolutionDiagnostic,
            CalculationDate = date,
            Schedule = schedule,
            Marks = marks,
            NextDayMarks = nextDayMarks,
            Parameters = parameters,
            Exceptions = exceptions,
            IsHoliday = holiday.IsHoliday,
            HolidayName = holiday.HolidayName,
            IsWeekend = IsWeekend(date, parameters.WeekendsRaw),
            IsNoSchedule = schedule is null || !schedule.HasSchedule
        };
    }

    // [ASISTWEB][SEC.01.02]
    private static bool IsWeekend(DateOnly date, string? weekendsRaw)
    {
        if (string.IsNullOrWhiteSpace(weekendsRaw))
        {
            return false;
        }

        var dayValue = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            DayOfWeek.Saturday => 7,
            _ => 0
        };

        if (dayValue == 0)
        {
            return false;
        }

        return weekendsRaw
            .Split(',', StringSplitOptions.TrimEntries)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => int.TryParse(x, out var parsed) ? parsed : 0)
            .Any(value => value == dayValue);
    }
}
