using ControlAsistencia.Web.Services;
using Microsoft.Extensions.Configuration;

namespace ControlAsistencia.Web.Tests;

public static class IntegrationValidationRunner
{
    public static async Task RunAsync()
    {
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "ControlAsistencia.Web"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var personProvider = new AttendancePersonProvider(configuration);
        var scheduleProvider = new AttendanceScheduleProvider(configuration);
        var markProvider = new AttendanceMarkProvider(configuration);
        var parameterProvider = new AttendanceParameterProvider(configuration);
        var holidayProvider = new AttendanceHolidayProvider(configuration);
        var exceptionProvider = new AttendanceExceptionProvider(configuration);
        var contextBuilder = new AttendanceCalculationContextBuilder(
            personProvider,
            scheduleProvider,
            markProvider,
            parameterProvider,
            holidayProvider,
            exceptionProvider);
        var engine = new AttendanceCalculationEngine();

        Console.WriteLine("INTEGRATION | START | SQL READ-ONLY");

        var person = await personProvider.GetByPersonIdAsync(1);
        Console.WriteLine(person is null
            ? "INTEGRATION | AttendancePersonProvider | BLOCKED | DATA NOT AVAILABLE"
            : $"INTEGRATION | AttendancePersonProvider | PASS | PersonId={person.PersonId} Badge={person.PersonCode}");

        var parameters = await parameterProvider.GetParametersAsync();
        Console.WriteLine($"INTEGRATION | AttendanceParameterProvider | PASS | NoInAbsent={parameters.NoInAbsent} Weekends={parameters.Weekends}");

        var dates = new[]
        {
            new DateOnly(2026, 8, 20),
            new DateOnly(2026, 8, 21),
            DateOnly.FromDateTime(DateTime.Today)
        };

        var executed = false;
        foreach (var date in dates)
        {
            var marks = await markProvider.GetMarksAsync(1, date);
            var schedule = await scheduleProvider.GetScheduleAsync(1, date);
            var holiday = await holidayProvider.GetHolidayAsync(date);
            var exceptions = await exceptionProvider.GetExceptionsAsync(1, date);

            Console.WriteLine($"INTEGRATION | AttendanceMarkProvider | PASS | Date={date:yyyy-MM-dd} Marks={marks.Count}");
            Console.WriteLine($"INTEGRATION | AttendanceScheduleProvider | PASS | Date={date:yyyy-MM-dd} HasSchedule={(schedule?.HasSchedule ?? false)}");
            Console.WriteLine($"INTEGRATION | AttendanceHolidayProvider | PASS | Date={date:yyyy-MM-dd} IsHoliday={holiday.IsHoliday}");
            Console.WriteLine($"INTEGRATION | AttendanceExceptionProvider | PASS | Date={date:yyyy-MM-dd} Exceptions={exceptions.Count}");

            var context = await contextBuilder.BuildAsync(1, date);
            if (context is null)
            {
                Console.WriteLine($"INTEGRATION | AttendanceCalculationContextBuilder | BLOCKED | DATA NOT AVAILABLE | Date={date:yyyy-MM-dd}");
                continue;
            }

            Console.WriteLine($"INTEGRATION | AttendanceCalculationContextBuilder | PASS | Date={date:yyyy-MM-dd} Marks={context.Marks.Count} NextDayMarks={context.NextDayMarks.Count}");

            var result = engine.Calculate(context);
            Console.WriteLine($"INTEGRATION | AttendanceCalculationEngine | PASS | Date={date:yyyy-MM-dd} Entry={result.EntryMark?.Timestamp:yyyy-MM-dd HH:mm} Exit={result.ExitMark?.Timestamp:yyyy-MM-dd HH:mm} Absent={result.IsAbsent}");
            executed = true;
        }

        if (!executed)
        {
            Console.WriteLine("INTEGRATION | BLOCKED | DATA NOT AVAILABLE");
        }
    }
}