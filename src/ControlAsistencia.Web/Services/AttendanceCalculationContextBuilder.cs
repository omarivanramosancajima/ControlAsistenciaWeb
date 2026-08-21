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

    public async Task<AttendanceCalculationContext?> BuildAsync(int personId, DateOnly date)
    {
        var person = await _personProvider.GetByPersonIdAsync(personId);
        if (person is null)
        {
            return null;
        }

        var schedule = await _scheduleProvider.GetScheduleAsync(personId, date);
        var marks = await _markProvider.GetMarksAsync(personId, date);
        var nextDayMarks = await _markProvider.GetMarksAsync(personId, date.AddDays(1));
        var parameters = await _parameterProvider.GetParametersAsync();
        var holiday = await _holidayProvider.GetHolidayAsync(date);
        var exceptions = await _exceptionProvider.GetExceptionsAsync(personId, date);

        return new AttendanceCalculationContext
        {
            PersonId = person.PersonId,
            PersonCode = person.PersonCode,
            PersonDocumentNumber = person.PersonDocumentNumber,
            PersonName = person.PersonName,
            DepartmentId = person.DepartmentId,
            DepartmentName = person.DepartmentName,
            CalculationDate = date,
            Schedule = schedule,
            Marks = marks,
            NextDayMarks = nextDayMarks,
            Parameters = parameters,
            Exceptions = exceptions,
            IsHoliday = holiday.IsHoliday,
            HolidayName = holiday.HolidayName,
            IsWeekend = IsWeekend(date, parameters.Weekends),
            IsNoSchedule = schedule is null || !schedule.HasSchedule
        };
    }

    private static bool IsWeekend(DateOnly date, int weekends)
    {
        var dayBit = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 4,
            DayOfWeek.Wednesday => 8,
            DayOfWeek.Thursday => 16,
            DayOfWeek.Friday => 32,
            DayOfWeek.Saturday => 64,
            _ => 0
        };

        return (weekends & dayBit) == dayBit;
    }
}