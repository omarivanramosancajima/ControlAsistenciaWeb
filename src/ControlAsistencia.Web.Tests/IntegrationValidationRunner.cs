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
        Console.WriteLine($"INTEGRATION | AttendanceParameterProvider | PASS | NoInAbsent={parameters.NoInAbsent} WeekendsRaw={parameters.WeekendsRaw}");

        var dates = new[]
        {
            new DateOnly(2026, 8, 20),
            new DateOnly(2026, 8, 21),
            DateOnly.FromDateTime(DateTime.Today)
        };

        var from = new DateTime(2026, 8, 20);
        var to = new DateTime(2026, 8, 22);
        var context = await contextBuilder.BuildAsync(1, from, to);
        if (context is null)
        {
            Console.WriteLine("INTEGRATION | AttendanceCalculationContextBuilder | BLOCKED | DATA NOT AVAILABLE");
        }
        else
        {
            Console.WriteLine($"INTEGRATION | AttendanceCalculationContextBuilder | PASS | Days={context.Days.Count}");
            var calculation = engine.Calculate(context);
            Console.WriteLine($"INTEGRATION | AttendanceCalculationEngine | PASS | DaysReturned={calculation.Days.Count}");
            executed = true;
        }

        if (!executed)
        {
            Console.WriteLine("INTEGRATION | BLOCKED | DATA NOT AVAILABLE");
        }
    }
}