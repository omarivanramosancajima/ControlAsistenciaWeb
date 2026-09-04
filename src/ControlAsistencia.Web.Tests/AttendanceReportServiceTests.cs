using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using ControlAsistencia.Web.Services;

namespace ControlAsistencia.Web.Tests;

public static class AttendanceReportServiceTests
{
    public static async Task RunAsync()
    {
        await Report_ConsumesAttendanceDayResult_WithoutRecalculation();
        await Report_DoesNotHide_NoSchedule_Holiday_Weekend_AndException();
        await Report_PreservesFilters_AndFormatsMoreThan24Hours();
        await Report_FiltersRequiredStates_AndCombination();
        await Report_ValidatesDateRangeRules();
        await Report_ExportUsesFullFilteredSet_NotCurrentPage();
    }

    private static async Task Report_ConsumesAttendanceDayResult_WithoutRecalculation()
    {
        var repository = new FakeReportRepository();
        var results = new Dictionary<DateOnly, AttendanceDayResult>
        {
            [new DateOnly(2026, 8, 20)] = CreateResult(new DateOnly(2026, 8, 20), accumulation: new AttendancePersonAccumulation
            {
                DiasDeAsistencia = 1,
                DiasConFalta = 0,
                HorasEfectivas = TimeSpan.FromHours(119) + TimeSpan.FromMinutes(6),
                HorasDePermanencia = TimeSpan.FromHours(7),
                TardanzasDelDia = TimeSpan.FromMinutes(5),
                SalidasTempranoDelDia = TimeSpan.FromMinutes(10),
                HorasExtras = TimeSpan.FromHours(2),
                DiasJustificados = 1,
                DiasProgramadosConTurno = 1,
                DiasDeAsistenciaSinTurno = 0,
                FeriadosConTurno = 0,
                FeriadosSinTurno = 0,
                HorasJustificadas = TimeSpan.FromHours(1)
            })
        };

        var service = CreateService(repository, results);
        var report = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 20),
            FechaHasta = new DateTime(2026, 8, 20)
        });

        Assert(report.Rows.Count == 1, "Debe generar una fila desde AttendanceDayResult.");
        Assert(report.Rows[0].HorasEfectivas == "119:06", "FormatDuration debe preservar horas >24h.");
        Assert(report.Rows[0].HorasDePermanencia == "109:16", "FormatDuration debe preservar horas >24h.");
        Assert(report.Rows[0].HorasPermiso == "01:00", "debe venir de JustifiedDuration del resultado final.");
        Assert(report.Rows[0].Excepcion == "Olvido de Marcaje", "Excepción debe mapearse desde ExceptionDisplayText/result.Exception.");
        Assert(report.Persons[0].HorasEfectivas == "119:06", "Acumulado debe venir de AttendancePersonAccumulation del motor.");
        Assert(report.Persons[0].HorasPermiso == "7:00", "Resumen HorasPermiso debe venir de HorasDePermanencia del acumulado del motor.");
        Assert(report.Persons[0].HorasJustificadas == "1:00", "Resumen de horas justificadas debe venir del acumulado del motor.");
    }

    private static async Task Report_DoesNotHide_NoSchedule_Holiday_Weekend_AndException()
    {
        var repository = new FakeReportRepository();
        var results = new Dictionary<DateOnly, AttendanceDayResult>
        {
            [new DateOnly(2026, 8, 20)] = CreateResult(new DateOnly(2026, 8, 20), isNoSchedule: true, accumulation: BaseAccumulation(1)),
            [new DateOnly(2026, 8, 21)] = CreateResult(new DateOnly(2026, 8, 21), isHoliday: true, isHolidayWithoutSchedule: true, accumulation: BaseAccumulation(2)),
            [new DateOnly(2026, 8, 22)] = CreateResult(new DateOnly(2026, 8, 22), isWeekend: true, accumulation: BaseAccumulation(3)),
            [new DateOnly(2026, 8, 23)] = CreateResult(new DateOnly(2026, 8, 23), exceptionText: "Compensación Días", accumulation: BaseAccumulation(4))
        };

        var service = CreateService(repository, results);
        var report = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 20),
            FechaHasta = new DateTime(2026, 8, 23)
        });

        Assert(report.EstadosDisponibles.Contains("Sin turno"), "Debe conservar el estado Sin turno.");
        Assert(report.EstadosDisponibles.Contains("Feriado con turno"), "Debe conservar el estado Feriado con turno.");
        Assert(report.EstadosDisponibles.Contains("Feriado sin turno"), "Debe conservar el estado Feriado sin turno.");
        Assert(report.EstadosDisponibles.Contains("FDS"), "Debe conservar el estado FDS.");
        Assert(report.EstadosDisponibles.Contains("Excepción"), "Debe conservar el estado Excepción.");
    }

    private static async Task Report_PreservesFilters_AndFormatsMoreThan24Hours()
    {
        var repository = new FakeReportRepository();
        var results = new Dictionary<DateOnly, AttendanceDayResult>
        {
            [new DateOnly(2026, 8, 20)] = CreateResult(new DateOnly(2026, 8, 20), late: TimeSpan.FromMinutes(45), justified: TimeSpan.FromMinutes(30), accumulation: new AttendancePersonAccumulation
            {
                DiasDeAsistencia = 2,
                DiasConFalta = 0,
                HorasEfectivas = TimeSpan.FromHours(119) + TimeSpan.FromMinutes(6),
                HorasDePermanencia = TimeSpan.FromHours(12),
                TardanzasDelDia = TimeSpan.FromMinutes(45),
                SalidasTempranoDelDia = TimeSpan.Zero,
                HorasExtras = TimeSpan.FromHours(4),
                DiasJustificados = 1,
                DiasProgramadosConTurno = 2,
                DiasDeAsistenciaSinTurno = 0,
                FeriadosConTurno = 0,
                FeriadosSinTurno = 0,
                HorasJustificadas = TimeSpan.FromMinutes(30)
            })
        };

        var service = CreateService(repository, results);
        var report = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 20),
            FechaHasta = new DateTime(2026, 8, 20),
            Persona = "Persona Demo",
            Area = "Sistemas",
            Estado = "Tardanza"
        });

        Assert(report.Rows.Count == 1, "Los filtros Persona + Área + Estado deben conservarse.");
        Assert(report.Rows[0].TardanzaEntrada == "00:45", "Debe mapear tardanza desde LateEntryDuration.");
        Assert(report.Persons[0].HorasEfectivas == "119:06", "Acumulados >24h deben conservarse en el resumen.");
    }

    private static async Task Report_FiltersRequiredStates_AndCombination()
    {
        var repository = new FakeReportRepository();
        var results = new Dictionary<DateOnly, AttendanceDayResult>
        {
            [new DateOnly(2026, 8, 20)] = CreateResult(new DateOnly(2026, 8, 20), isAbsent: true, exceptionText: string.Empty, accumulation: BaseAccumulation(1)),
            [new DateOnly(2026, 8, 21)] = CreateResult(new DateOnly(2026, 8, 21), late: TimeSpan.FromMinutes(12), exceptionText: string.Empty, accumulation: BaseAccumulation(2)),
            [new DateOnly(2026, 8, 22)] = CreateResult(new DateOnly(2026, 8, 22), earlyExit: TimeSpan.FromMinutes(8), exceptionText: string.Empty, accumulation: BaseAccumulation(3)),
            [new DateOnly(2026, 8, 23)] = CreateResult(new DateOnly(2026, 8, 23), overtime: TimeSpan.FromHours(3), exceptionText: string.Empty, accumulation: BaseAccumulation(4)),
            [new DateOnly(2026, 8, 24)] = CreateResult(new DateOnly(2026, 8, 24), justified: TimeSpan.FromHours(2), exceptionText: "Permiso Médico", accumulation: BaseAccumulation(5)),
            [new DateOnly(2026, 8, 25)] = CreateResult(new DateOnly(2026, 8, 25), isNoSchedule: true, withMarks: true, exceptionText: string.Empty, accumulation: BaseAccumulation(6)),
            [new DateOnly(2026, 8, 26)] = CreateResult(new DateOnly(2026, 8, 26), isHoliday: true, exceptionText: string.Empty, accumulation: BaseAccumulation(7))
        };

        var service = CreateService(repository, results);

        await AssertFilterCount(service, "FALTAS", 1);
        await AssertFilterCount(service, "TARDANZAS", 1);
        await AssertFilterCount(service, "SALIDAS TEMPRANAS", 1);
        await AssertFilterCount(service, "HORAS EXTRA", 1);
        await AssertFilterCount(service, "PERMISOS/JUSTIFICACIONES", 1);
        await AssertFilterCount(service, "ASISTENCIA SIN TURNO", 1);
        await AssertFilterCount(service, "FERIADOS", 1);

        var combinedReport = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 20),
            FechaHasta = new DateTime(2026, 8, 26),
            Persona = "Persona Demo",
            Area = "Sistemas",
            Estado = "HORAS EXTRA"
        });

        Assert(combinedReport.Rows.Count == 1, "La combinación Persona + Área + Estado debe respetarse.");
        Assert(combinedReport.Rows[0].HorasExtras == "03:00", "El filtro combinado debe devolver la fila correcta derivada de AttendanceDayResult.");
    }

    private static async Task Report_ValidatesDateRangeRules()
    {
        var service = CreateService(new FakeReportRepository(), new Dictionary<DateOnly, AttendanceDayResult>());

        await AssertThrowsAsync(
            () => service.GetReportAsync(new AttendanceReportRequest
            {
                FechaDesde = new DateTime(2026, 8, 10),
                FechaHasta = new DateTime(2026, 8, 9)
            }),
            "FechaDesde");

        await AssertThrowsAsync(
            () => service.GetReportAsync(new AttendanceReportRequest
            {
                FechaDesde = new DateTime(2026, 8, 1),
                FechaHasta = new DateTime(2026, 10, 5)
            }),
            "62");
    }

    private static async Task Report_ExportUsesFullFilteredSet_NotCurrentPage()
    {
        var repository = new FakeReportRepository();
        var results = new Dictionary<DateOnly, AttendanceDayResult>();

        for (var day = 1; day <= 25; day++)
        {
            results[new DateOnly(2026, 8, day)] = CreateResult(new DateOnly(2026, 8, day), overtime: TimeSpan.FromHours(1), exceptionText: string.Empty, accumulation: BaseAccumulation(day));
        }

        var service = CreateService(repository, results);
        var pageReport = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 1),
            FechaHasta = new DateTime(2026, 8, 25),
            Estado = "HORAS EXTRA",
            PageNumber = 2,
            PageSize = 20
        });

        var exportReport = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 1),
            FechaHasta = new DateTime(2026, 8, 25),
            Estado = "HORAS EXTRA",
            PageNumber = 1,
            PageSize = int.MaxValue
        });

        Assert(pageReport.Rows.Count == 5, "La segunda página debe contener el remanente del conjunto filtrado.");
        Assert(exportReport.Rows.Count == 25, "La exportación debe incluir todo el conjunto filtrado, no solo la página actual.");
    }

    private static AttendanceReportService CreateService(FakeReportRepository repository, IDictionary<DateOnly, AttendanceDayResult> results)
        => new(repository, new FakeContextBuilder(results), new FakeCalculationEngine(results));

    private static async Task AssertFilterCount(AttendanceReportService service, string estado, int expected)
    {
        var report = await service.GetReportAsync(new AttendanceReportRequest
        {
            FechaDesde = new DateTime(2026, 8, 20),
            FechaHasta = new DateTime(2026, 8, 26),
            Estado = estado
        });

        Assert(report.Rows.Count == expected, $"El filtro {estado} debe devolver {expected} registro(s).");
    }

    private static async Task AssertThrowsAsync(Func<Task> action, string expectedMessage)
    {
        try
        {
            await action();
            throw new InvalidOperationException("Se esperaba una excepción de validación.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static AttendanceDayResult CreateResult(
        DateOnly date,
        bool isNoSchedule = false,
        bool isHoliday = false,
        bool isHolidayWithoutSchedule = false,
        bool isWeekend = false,
        bool isAbsent = false,
        bool withMarks = false,
        TimeSpan? late = null,
        TimeSpan? earlyExit = null,
        TimeSpan? overtime = null,
        TimeSpan? justified = null,
        string exceptionText = "Olvido de Marcaje",
        AttendancePersonAccumulation? accumulation = null)
    {
        var hasOperationalMarks = withMarks || !isNoSchedule;

        return new AttendanceDayResult
        {
            PersonId = 1,
            PersonCode = "1001",
            PersonDocumentNumber = "12345678",
            PersonName = "Persona Demo",
            DepartmentId = 10,
            DepartmentName = "Sistemas",
            CompanyTaxId = "20123456789",
            CompanyName = "FabricaSoft",
            Date = date,
            DayNumberText = date.Day.ToString("00"),
            DayNameText = date.DayOfWeek.ToString(),
            Schedule = isNoSchedule ? new AttendanceSchedule { HasSchedule = false } : new AttendanceSchedule
            {
                HasSchedule = true,
                Code = "HR01",
                ScheduledStartTime = new TimeSpan(8, 0, 0),
                ScheduledEndTime = new TimeSpan(17, 0, 0)
            },
            EntryMark = isAbsent ? null : (hasOperationalMarks ? new AttendanceMark { Timestamp = date.ToDateTime(new TimeOnly(8, 0)) } : null),
            ExitMark = isAbsent ? null : (hasOperationalMarks ? new AttendanceMark { Timestamp = date.ToDateTime(new TimeOnly(17, 0)) } : null),
            IntermediateMarks = isAbsent
                ? Array.Empty<AttendanceMark>()
                : (hasOperationalMarks ? new[] { new AttendanceMark { Timestamp = date.ToDateTime(new TimeOnly(13, 0)) } } : Array.Empty<AttendanceMark>()),
            IsAbsent = isAbsent,
            EffectiveWorkDuration = isAbsent ? null : TimeSpan.FromHours(119) + TimeSpan.FromMinutes(6),
            PresenceDuration = TimeSpan.FromHours(8),
            LateEntryDuration = late,
            EarlyExitDuration = earlyExit,
            OvertimeDuration = overtime,
            Exception = string.IsNullOrWhiteSpace(exceptionText) ? null : new AttendanceException { LeaveName = exceptionText },
            JustifiedDuration = justified,
            JustifiedDayFraction = justified.HasValue && justified.Value > TimeSpan.Zero ? 1m : 0m,
            IsHoliday = isHoliday,
            IsWeekend = isWeekend,
            IsHolidayWithSchedule = isHoliday && !isHolidayWithoutSchedule && !isNoSchedule,
            IsHolidayWithoutSchedule = isHolidayWithoutSchedule,
            IsNoSchedule = isNoSchedule,
            HasScheduledAssignment = !isNoSchedule,
            HasExceptions = !string.IsNullOrWhiteSpace(exceptionText),
            ExceptionDisplayText = exceptionText,
            ScheduleDisplayText = isNoSchedule ? "SIN TURNO" : "HR01 08:00 - 17:00",
            Accumulation = accumulation ?? BaseAccumulation(1)
        };
    }

    private static AttendancePersonAccumulation BaseAccumulation(int attendanceDays)
        => new()
        {
            DiasDeAsistencia = attendanceDays,
            DiasProgramadosConTurno = attendanceDays,
            HorasEfectivas = TimeSpan.FromHours(attendanceDays),
            HorasDePermanencia = TimeSpan.FromHours(attendanceDays),
            HorasJustificadas = TimeSpan.Zero
        };

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class FakeReportRepository : IAttendanceReportRepository
    {
        public Task<IReadOnlyList<string>> GetAvailableAreasAsync()
            => Task.FromResult<IReadOnlyList<string>>(new[] { "Sistemas" });

        public Task<AttendanceReportCompanyInfo?> GetCompanyInfoAsync()
            => Task.FromResult<AttendanceReportCompanyInfo?>(new AttendanceReportCompanyInfo { TaxId = "20123456789", CompanyName = "FabricaSoft" });

        public Task<IReadOnlyList<AttendanceReportFilterPerson>> GetFilterPersonsAsync(string? personName, string? areaName)
            => Task.FromResult<IReadOnlyList<AttendanceReportFilterPerson>>(new[]
            {
                new AttendanceReportFilterPerson
                {
                    PersonId = 1,
                    PersonCode = "1001",
                    PersonDocumentNumber = "12345678",
                    PersonName = "Persona Demo",
                    DepartmentId = 10,
                    DepartmentName = "Sistemas"
                }
            });
    }

    private sealed class FakeContextBuilder : IAttendanceCalculationContextBuilder
    {
        private readonly IDictionary<DateOnly, AttendanceDayResult> _results;

        public FakeContextBuilder(IDictionary<DateOnly, AttendanceDayResult> results)
        {
            _results = results;
        }

        public Task<AttendanceCalculationContext?> BuildAsync(int personId, DateTime from, DateTime to)
        {
            var dates = _results.Keys
                .Where(date => date >= DateOnly.FromDateTime(from.Date) && date <= DateOnly.FromDateTime(to.Date))
                .OrderBy(date => date)
                .Select(date => new AttendanceCalculationDayContext
                {
                    PersonId = personId,
                    PersonCode = "1001",
                    PersonDocumentNumber = "12345678",
                    PersonName = "Persona Demo",
                    DepartmentId = 10,
                    DepartmentName = "Sistemas",
                    CalculationDate = date
                })
                .ToList();

            return Task.FromResult<AttendanceCalculationContext?>(new AttendanceCalculationContext
            {
                PersonContext = new AttendancePersonContext
                {
                    PersonId = personId,
                    PersonCode = "1001",
                    PersonDocumentNumber = "12345678",
                    PersonName = "Persona Demo",
                    DepartmentId = 10,
                    DepartmentName = "Sistemas"
                },
                FechaDesde = DateOnly.FromDateTime(from.Date),
                FechaHasta = DateOnly.FromDateTime(to.Date),
                Days = dates
            });
        }
    }

    private sealed class FakeCalculationEngine : IAttendanceCalculationEngine
    {
        private readonly IDictionary<DateOnly, AttendanceDayResult> _results;

        public FakeCalculationEngine(IDictionary<DateOnly, AttendanceDayResult> results)
        {
            _results = results;
        }

        public AttendanceCalculationResult Calculate(AttendanceCalculationContext context)
        {
            var days = context.Days
                .Where(day => _results.ContainsKey(day.CalculationDate))
                .OrderBy(day => day.CalculationDate)
                .Select(day => _results[day.CalculationDate])
                .ToList();

            var accumulation = days.Count > 0
                ? days[^1].Accumulation
                : new AttendancePersonAccumulation();

            return new AttendanceCalculationResult
            {
                PersonContext = context.PersonContext,
                Days = days,
                Accumulation = accumulation
            };
        }

        public AttendanceDayResult CalculateDay(AttendanceCalculationDayContext context)
            => _results[context.CalculationDate];
    }

}