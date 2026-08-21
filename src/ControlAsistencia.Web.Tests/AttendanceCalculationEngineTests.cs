using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Services;

namespace ControlAsistencia.Web.Tests;

public static class AttendanceCalculationEngineTests
{
    public static void Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("Caso 01: Día normal con dos marcas", DayWithTwoMarks_UsesWindowedEntryAndExit),
            ("Caso 02: Entrada tardía", LateEntry_ComputesDuration),
            ("Caso 03: Salida temprana", EarlyExit_ComputesDuration),
            ("Caso 04: Entrada faltante", MissingEntry_NoInAbsent2_IsAbsent),
            ("Caso 05: Salida faltante", MissingExit_NoOutAbsent2_IsAbsent),
            ("Caso 06: Entrada y salida faltantes", ZeroMarks_WithSchedule_IsAbsent),
            ("Caso 07: Cero marcas", ZeroMarks_NoSchedule_NoCalculations),
            ("Caso 08: NoInAbsent = 0", MissingEntry_NoInAbsent0_UsesScheduledStart),
            ("Caso 09: NoInAbsent = 1", MissingEntry_NoInAbsent1_DefaultLate),
            ("Caso 10: NoInAbsent = 2", MissingEntry_NoInAbsent2_Inconsistency),
            ("Caso 11: NoOutAbsent = 0", MissingExit_NoOutAbsent0_UsesScheduledEnd),
            ("Caso 12: NoOutAbsent = 1", MissingExit_NoOutAbsent1_DefaultEarly),
            ("Caso 13: NoOutAbsent = 2", MissingExit_NoOutAbsent2_Inconsistency),
            ("Caso 14: Sin turno + múltiples marcas", NoSchedule_MultipleMarks_UsesFirstAndLast),
            ("Caso 15: Sin turno + una marca + L siguiente", NoSchedule_SingleMarkWithNextDayClosure_UsesNextDayExit),
            ("Caso 16: Sin turno + una marca sin L siguiente", NoSchedule_SingleMarkWithoutNextDayClosure_Inconsistency),
            ("Caso 17: Turno con múltiples marcas", ScheduledDay_MultipleMarks_PreservesIntermediateMarks),
            ("Caso 18: Primera entrada dentro de ventana", EntryWindow_SelectsFirstValidMark),
            ("Caso 19: Última salida dentro de ventana", ExitWindow_SelectsLastValidMark),
            ("Caso 20: Marcas fuera de ventana", MarksOutsideWindow_AreNotSelected),
            ("Caso 21: Horario con ventanas propias", ExplicitWindows_AreUsed),
            ("Caso 22: Horario sin ventanas propias", DefaultWindows_AreUsed),
            ("Caso 23: Amanecida", OvernightSchedule_ComputesAcrossMidnight),
            ("Caso 24: Cruce de medianoche", DefaultExitWindow_AllowsAfterMidnight),
            ("Caso 25: Feriado con turno", HolidayWithSchedule_UsesHolidayOvertime),
            ("Caso 26: Feriado sin turno", HolidayWithoutSchedule_UsesHolidayRules),
            ("Caso 27: Fin de semana con turno", WeekendWithSchedule_UsesWeekendOt),
            ("Caso 28: Fin de semana sin turno", WeekendWithoutSchedule_UsesWeekendOt),
            ("Caso 28B: Fin de semana con turno generalizado 09:00-17:00", WeekendWithSchedule_Generalized_UsesWeekendOt),
            ("Caso 29: H.E. antes", EarlyOvertime_IsCalculated),
            ("Caso 30: H.E. después", AfterOvertime_IsCalculated),
            ("Caso 31: Límite H.E.", Overtime_Limit_IsApplied),
            ("Caso 32: Excepción completa", FullDayException_RemovesAbsence),
            ("Caso 33: Excepción parcial", PartialException_ReducesLateDuration),
            ("Caso 34: Excepciones solapadas", OverlappingExceptions_ClassifyZeroHasPriority),
            ("Caso 35: Classify=0", ClassifyZero_IsSelected),
            ("Caso 36: Inconsistencia por entrada faltante", MissingEntry_SetsInconsistency),
            ("Caso 37: Inconsistencia por salida faltante", MissingExit_SetsInconsistency),
            ("Caso 38: Inconsistencia por una marca sin L siguiente", SingleMarkWithoutL_SetsInconsistency)
        };

        var passed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Execute();
                Console.WriteLine($"PASS | {test.Name}");
                passed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL | {test.Name} | {ex.Message}");
            }
        }

        Console.WriteLine($"Resumen: {passed}/{tests.Length} pruebas PASS");
    }

    private static void DayWithTwoMarks_UsesWindowedEntryAndExit()
    {
        var result = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 5, 0), new DateTime(2026, 8, 20, 17, 55, 0)));
        Assert(result.EntryMark?.Timestamp == new DateTime(2026, 8, 20, 8, 5, 0), "EntryMark inválida.");
        Assert(result.ExitMark?.Timestamp == new DateTime(2026, 8, 20, 17, 55, 0), "ExitMark inválida.");
    }

    private static void LateEntry_ComputesDuration()
    {
        var result = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 20, 0), new DateTime(2026, 8, 20, 18, 0, 0)));
        Assert(result.LateEntryDuration == TimeSpan.FromMinutes(10), "Tardanza esperada 10 min.");
    }

    private static void EarlyExit_ComputesDuration()
    {
        var result = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 17, 40, 0)));
        Assert(result.EarlyExitDuration == TimeSpan.FromMinutes(10), "Salida temprana esperada 10 min.");
    }

    private static void MissingEntry_NoInAbsent2_IsAbsent()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0));
        context.Parameters.NoInAbsent = 2;
        var result = CreateEngine().Calculate(context);
        Assert(result.IsAbsent, "Debe ser falta.");
    }

    private static void MissingExit_NoOutAbsent2_IsAbsent()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null);
        context.Parameters.NoOutAbsent = 2;
        var result = CreateEngine().Calculate(context);
        Assert(result.IsAbsent, "Debe ser falta.");
    }

    private static void ZeroMarks_WithSchedule_IsAbsent()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, null);
        context.Marks = Array.Empty<AttendanceMark>();
        var result = CreateEngine().Calculate(context);
        Assert(result.IsAbsent, "Con turno y cero marcas debe ser falta.");
    }

    private static void ZeroMarks_NoSchedule_NoCalculations()
    {
        var result = CreateEngine().Calculate(CreateNoScheduleContext(Array.Empty<AttendanceMark>(), Array.Empty<AttendanceMark>()));
        Assert(!result.IsAbsent && result.EntryMark is null && result.ExitMark is null, "Sin turno y sin marcas no debe calcular.");
    }

    private static void MissingEntry_NoInAbsent0_UsesScheduledStart()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0));
        context.Parameters.NoInAbsent = 0;
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark is null, "No debe crear AttendanceMark artificial de entrada.");
        Assert(result.EffectiveWorkDuration == TimeSpan.FromHours(10), "Debe calcular usando hora programada de entrada como sustitución funcional.");
    }

    private static void MissingEntry_NoInAbsent1_DefaultLate()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0));
        context.Parameters.NoInAbsent = 1;
        context.Parameters.MinsNoIn = 54;
        var result = CreateEngine().Calculate(context);
        Assert(result.LateEntryDuration == TimeSpan.FromMinutes(54), "Debe usar tardanza por defecto.");
    }

    private static void MissingEntry_NoInAbsent2_Inconsistency()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0));
        context.Parameters.NoInAbsent = 2;
        var result = CreateEngine().Calculate(context);
        Assert(result.InconsistencyKind == AttendanceInconsistencyKind.MissingEntry, "Debe marcar inconsistencia por entrada faltante.");
    }

    private static void MissingExit_NoOutAbsent0_UsesScheduledEnd()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null);
        context.Parameters.NoOutAbsent = 0;
        var result = CreateEngine().Calculate(context);
        Assert(result.ExitMark is null, "No debe crear AttendanceMark artificial de salida.");
        Assert(result.EffectiveWorkDuration == TimeSpan.FromHours(10), "Debe calcular usando hora programada de salida como sustitución funcional.");
    }

    private static void MissingExit_NoOutAbsent1_DefaultEarly()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null);
        context.Parameters.NoOutAbsent = 1;
        context.Parameters.MinsNoLeave = 44;
        var result = CreateEngine().Calculate(context);
        Assert(result.EarlyExitDuration == TimeSpan.FromMinutes(44), "Debe usar salida temprana por defecto.");
    }

    private static void MissingExit_NoOutAbsent2_Inconsistency()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null);
        context.Parameters.NoOutAbsent = 2;
        var result = CreateEngine().Calculate(context);
        Assert(result.InconsistencyKind == AttendanceInconsistencyKind.MissingExit, "Debe marcar inconsistencia por salida faltante.");
    }

    private static void NoSchedule_MultipleMarks_UsesFirstAndLast()
    {
        var marks = new[]
        {
            Mark(new DateTime(2026,8,20,9,0,0)),
            Mark(new DateTime(2026,8,20,12,0,0)),
            Mark(new DateTime(2026,8,20,18,0,0))
        };
        var result = CreateEngine().Calculate(CreateNoScheduleContext(marks, Array.Empty<AttendanceMark>()));
        Assert(result.EntryMark?.Timestamp == marks[0].Timestamp && result.ExitMark?.Timestamp == marks[2].Timestamp, "Debe usar primera y última marca.");
    }

    private static void NoSchedule_SingleMarkWithNextDayClosure_UsesNextDayExit()
    {
        var marks = new[] { Mark(new DateTime(2026, 8, 20, 20, 0, 0)) };
        var nextDay = new[] { Mark(new DateTime(2026, 8, 21, 2, 0, 0), "L") };
        var result = CreateEngine().Calculate(CreateNoScheduleContext(marks, nextDay));
        Assert(result.ExitMark?.IsPreviousDayClosureMark == true, "Debe usar cierre del día siguiente.");
    }

    private static void NoSchedule_SingleMarkWithoutNextDayClosure_Inconsistency()
    {
        var marks = new[] { Mark(new DateTime(2026, 8, 20, 20, 0, 0)) };
        var result = CreateEngine().Calculate(CreateNoScheduleContext(marks, Array.Empty<AttendanceMark>()));
        Assert(result.InconsistencyKind == AttendanceInconsistencyKind.SingleMarkWithoutNextDayClosure, "Debe marcar inconsistencia.");
    }

    private static void ScheduledDay_MultipleMarks_PreservesIntermediateMarks()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20),
            new DateTime(2026, 8, 20, 8, 0, 0),
            new DateTime(2026, 8, 20, 18, 0, 0),
            new[] { Mark(new DateTime(2026, 8, 20, 12, 0, 0)) });
        var result = CreateEngine().Calculate(context);
        Assert(result.IntermediateMarks.Count == 1, "Debe conservar marca intermedia.");
    }

    private static void EntryWindow_SelectsFirstValidMark()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20),
            new DateTime(2026, 8, 20, 7, 55, 0),
            new DateTime(2026, 8, 20, 18, 0, 0));
        context.Schedule!.CheckInTime1 = new TimeOnly(7, 50);
        context.Schedule.CheckInTime2 = new TimeOnly(8, 10);
        context.Schedule.CheckOutTime1 = new TimeOnly(17, 0);
        context.Schedule.CheckOutTime2 = new TimeOnly(18, 30);
        context.Marks = new[]
        {
            Mark(new DateTime(2026,8,20,6,0,0)),
            Mark(new DateTime(2026,8,20,7,55,0)),
            Mark(new DateTime(2026,8,20,8,5,0)),
            Mark(new DateTime(2026,8,20,18,0,0))
        };
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark?.Timestamp == new DateTime(2026,8,20,7,55,0), "Debe escoger primera válida dentro de ventana.");
    }

    private static void ExitWindow_SelectsLastValidMark()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 17, 50, 0));
        context.Marks = new[]
        {
            Mark(new DateTime(2026,8,20,8,0,0)),
            Mark(new DateTime(2026,8,20,17,50,0)),
            Mark(new DateTime(2026,8,20,17,59,0))
        };
        var result = CreateEngine().Calculate(context);
        Assert(result.ExitMark?.Timestamp == new DateTime(2026,8,20,17,59,0), "Debe escoger última válida dentro de ventana.");
    }

    private static void MarksOutsideWindow_AreNotSelected()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 3, 0, 0), new DateTime(2026, 8, 20, 23, 30, 0));
        context.Parameters.NoInAbsent = 2;
        context.Parameters.NoOutAbsent = 2;
        var result = CreateEngine().Calculate(context);
        Assert(result.IsAbsent, "Marcas fuera de ventana no deben seleccionarse.");
    }

    private static void ExplicitWindows_AreUsed()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 45, 0), new DateTime(2026, 8, 20, 17, 40, 0));
        context.Schedule!.CheckInTime1 = new TimeOnly(8, 30);
        context.Schedule.CheckInTime2 = new TimeOnly(9, 0);
        context.Schedule.CheckOutTime1 = new TimeOnly(17, 30);
        context.Schedule.CheckOutTime2 = new TimeOnly(18, 0);
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark?.Timestamp.Hour == 8 && result.EntryMark.Timestamp.Minute == 45, "Debe usar ventana propia de entrada.");
    }

    private static void DefaultWindows_AreUsed()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 6, 30, 0), new DateTime(2026, 8, 20, 21, 30, 0));
        context.Schedule!.CheckInTime1 = null;
        context.Schedule.CheckInTime2 = null;
        context.Schedule.CheckOutTime1 = null;
        context.Schedule.CheckOutTime2 = null;
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark is not null && result.ExitMark is not null, "Debe usar ventanas por defecto.");
    }

    private static void OvernightSchedule_ComputesAcrossMidnight()
    {
        var context = CreateOvernightContext(new DateTime(2026, 8, 20, 22, 0, 0), new DateTime(2026, 8, 21, 7, 0, 0));
        var result = CreateEngine().Calculate(context);
        Assert(result.EffectiveWorkDuration == TimeSpan.FromHours(9), "Amanecida debe cruzar medianoche.");
    }

    private static void DefaultExitWindow_AllowsAfterMidnight()
    {
        var context = CreateOvernightContext(new DateTime(2026, 8, 20, 22, 10, 0), new DateTime(2026, 8, 21, 7, 10, 0));
        var result = CreateEngine().Calculate(context);
        Assert(result.ExitMark?.Timestamp.Date == new DateTime(2026, 8, 21).Date, "Debe permitir salida al día siguiente.");
    }

    private static void HolidayWithSchedule_UsesHolidayOvertime()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0));
        context.IsHoliday = true;
        context.Parameters.ShowHoliday = true;
        context.Parameters.AllowHolidayOT = true;
        context.Parameters.LimitHolidayOT = 60;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromMinutes(60), "HE feriado debe respetar límite.");
    }

    private static void HolidayWithoutSchedule_UsesHolidayRules()
    {
        var marks = new[] { Mark(new DateTime(2026, 8, 20, 9, 0, 0)), Mark(new DateTime(2026, 8, 20, 11, 0, 0)) };
        var context = CreateNoScheduleContext(marks, Array.Empty<AttendanceMark>());
        context.IsHoliday = true;
        context.Parameters.ShowHoliday = true;
        context.Parameters.AllowHolidayOT = true;
        context.Parameters.LimitHolidayOT = 60;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromMinutes(60), "Feriado sin turno debe usar reglas de feriado.");
    }

    private static void WeekendWithSchedule_UsesWeekendOt()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 23), new DateTime(2026, 8, 23, 8, 0, 0), new DateTime(2026, 8, 23, 18, 0, 0));
        context.IsWeekend = true;
        context.Parameters.WeekenFullDayOT = true;
        var result = CreateEngine().Calculate(context);
        Assert(result.EffectiveWorkDuration == TimeSpan.FromHours(10), "Fin de semana con turno debe mantener horas efectivas.");
        Assert(result.PresenceDuration == TimeSpan.FromHours(10), "Fin de semana con turno debe mantener permanencia.");
        Assert(result.OvertimeDuration == TimeSpan.FromHours(10), "Fin de semana con turno debe enviar toda la asistencia a HE según v01.02.");
    }

    private static void WeekendWithoutSchedule_UsesWeekendOt()
    {
        var marks = new[] { Mark(new DateTime(2026, 8, 23, 9, 0, 0)), Mark(new DateTime(2026, 8, 23, 12, 0, 0)) };
        var context = CreateNoScheduleContext(marks, Array.Empty<AttendanceMark>());
        context.IsWeekend = true;
        context.Parameters.WeekenFullDayOT = true;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromHours(3), "Fin de semana sin turno debe enviar toda la asistencia a HE.");
    }

    private static void WeekendWithSchedule_Generalized_UsesWeekendOt()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 24), new DateTime(2026, 8, 24, 9, 0, 0), new DateTime(2026, 8, 24, 17, 0, 0));
        context.Schedule!.ScheduledStartTime = new TimeOnly(9, 0);
        context.Schedule.ScheduledEndTime = new TimeOnly(17, 0);
        context.IsWeekend = true;
        context.Parameters.WeekenFullDayOT = true;
        var result = CreateEngine().Calculate(context);
        Assert(result.EffectiveWorkDuration == TimeSpan.FromHours(8), "Caso generalizado debe mantener horas efectivas.");
        Assert(result.PresenceDuration == TimeSpan.FromHours(8), "Caso generalizado debe mantener permanencia.");
        Assert(result.OvertimeDuration == TimeSpan.FromHours(8), "Caso generalizado debe reflejar la misma HE.");
    }

    private static void EarlyOvertime_IsCalculated()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 7, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0));
        context.Parameters.AllowEarlyOT = true;
        context.Parameters.IntervalOfEarlyOT = 30;
        context.Parameters.IntervalOfEarlyOTAlternate = 30;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromMinutes(30), "Debe calcular HE antes de entrada.");
    }

    private static void AfterOvertime_IsCalculated()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 19, 0, 0));
        context.Parameters.AllowAfterOT = true;
        context.Parameters.IntervalOfAfterOT = 30;
        context.Parameters.IntervalOfAfterOTAlternate = 30;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromMinutes(30), "Debe calcular HE después de salida.");
    }

    private static void Overtime_Limit_IsApplied()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 21, 0, 0));
        context.Parameters.AllowAfterOT = true;
        context.Parameters.IntervalOfAfterOT = 30;
        context.Parameters.IntervalOfAfterOTAlternate = 0;
        context.Parameters.LimitAfterMaxOT = true;
        context.Parameters.AfterMaxOT = 72;
        var result = CreateEngine().Calculate(context);
        Assert(result.OvertimeDuration == TimeSpan.FromMinutes(72), "Debe aplicar tope de HE.");
    }

    private static void FullDayException_RemovesAbsence()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), null, null);
        context.Marks = Array.Empty<AttendanceMark>();
        context.Exceptions = new[]
        {
            new AttendanceException
            {
                PersonId = context.PersonId,
                LeaveId = 1,
                LeaveName = "VACACIONES",
                Unit = 3,
                MinUnit = 1,
                Classify = 0,
                StartDateTime = new DateTime(2026,8,20,8,0,0),
                EndDateTime = new DateTime(2026,8,20,18,0,0)
            }
        };
        var result = CreateEngine().Calculate(context);
        Assert(!result.IsAbsent, "Excepción completa debe quitar falta.");
    }

    private static void PartialException_ReducesLateDuration()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 30, 0), new DateTime(2026, 8, 20, 18, 0, 0));
        context.Exceptions = new[]
        {
            new AttendanceException
            {
                PersonId = context.PersonId,
                LeaveId = 2,
                LeaveName = "PERMISO",
                Unit = 1,
                MinUnit = 1,
                Classify = 128,
                StartDateTime = new DateTime(2026,8,20,8,0,0),
                EndDateTime = new DateTime(2026,8,20,8,30,0)
            }
        };
        var result = CreateEngine().Calculate(context);
        Assert(result.LateEntryDuration is null, "Excepción parcial debe justificar tardanza solapada.");
    }

    private static void OverlappingExceptions_ClassifyZeroHasPriority()
    {
        var context = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 30, 0), new DateTime(2026, 8, 20, 18, 0, 0));
        context.Exceptions = new[]
        {
            new AttendanceException { PersonId = context.PersonId, LeaveId = 1, LeaveName = "A", Unit = 1, MinUnit = 1, Classify = 128, StartDateTime = new DateTime(2026,8,20,8,0,0), EndDateTime = new DateTime(2026,8,20,9,0,0) },
            new AttendanceException { PersonId = context.PersonId, LeaveId = 2, LeaveName = "B", Unit = 1, MinUnit = 1, Classify = 0, StartDateTime = new DateTime(2026,8,20,8,0,0), EndDateTime = new DateTime(2026,8,20,9,0,0) }
        };
        var result = CreateEngine().Calculate(context);
        Assert(result.Exception?.LeaveId == 2, "Classify=0 debe prevalecer.");
    }

    private static void ClassifyZero_IsSelected()
    {
        OverlappingExceptions_ClassifyZeroHasPriority();
    }

    private static void MissingEntry_SetsInconsistency()
    {
        MissingEntry_NoInAbsent2_Inconsistency();
    }

    private static void MissingExit_SetsInconsistency()
    {
        MissingExit_NoOutAbsent2_Inconsistency();
    }

    private static void SingleMarkWithoutL_SetsInconsistency()
    {
        NoSchedule_SingleMarkWithoutNextDayClosure_Inconsistency();
    }

    private static AttendanceCalculationEngine CreateEngine() => new();

    private static AttendanceCalculationContext CreateScheduledContext(DateOnly calculationDate, DateTime? entry, DateTime? exit, IEnumerable<AttendanceMark>? intermediateMarks = null)
    {
        var marks = new List<AttendanceMark>();
        if (entry.HasValue) marks.Add(Mark(entry.Value));
        if (intermediateMarks is not null) marks.AddRange(intermediateMarks);
        if (exit.HasValue) marks.Add(Mark(exit.Value));

        return new AttendanceCalculationContext
        {
            PersonId = 1,
            PersonCode = "P001",
            CalculationDate = calculationDate,
            Schedule = new AttendanceSchedule
            {
                HasSchedule = true,
                ScheduleName = "HS-MANANA",
                ScheduledStartTime = new TimeOnly(8, 0),
                ScheduledEndTime = new TimeOnly(18, 0),
                StartDayOffset = 1,
                EndDayOffset = 1,
                LateToleranceMinutes = 10,
                EarlyToleranceMinutes = 10
            },
            Marks = marks.OrderBy(x => x.Timestamp).ToList(),
            NextDayMarks = Array.Empty<AttendanceMark>(),
            Parameters = DefaultParameters(),
            Exceptions = Array.Empty<AttendanceException>()
        };
    }

    private static AttendanceCalculationContext CreateOvernightContext(DateTime entry, DateTime exit)
    {
        return new AttendanceCalculationContext
        {
            PersonId = 1,
            PersonCode = "P001",
            CalculationDate = new DateOnly(2026, 8, 20),
            Schedule = new AttendanceSchedule
            {
                HasSchedule = true,
                ScheduleName = "HS-NOCHE",
                ScheduledStartTime = new TimeOnly(22, 0),
                ScheduledEndTime = new TimeOnly(7, 0),
                StartDayOffset = 1,
                EndDayOffset = 2,
                IsOvernight = true,
                LateToleranceMinutes = 0,
                EarlyToleranceMinutes = 0
            },
            Marks = new[] { Mark(entry), Mark(exit) },
            NextDayMarks = Array.Empty<AttendanceMark>(),
            Parameters = DefaultParameters(),
            Exceptions = Array.Empty<AttendanceException>()
        };
    }

    private static AttendanceCalculationContext CreateNoScheduleContext(IReadOnlyList<AttendanceMark> marks, IReadOnlyList<AttendanceMark> nextDayMarks)
    {
        return new AttendanceCalculationContext
        {
            PersonId = 1,
            PersonCode = "P001",
            CalculationDate = new DateOnly(2026, 8, 20),
            Schedule = null,
            IsNoSchedule = true,
            Marks = marks,
            NextDayMarks = nextDayMarks,
            Parameters = DefaultParameters(),
            Exceptions = Array.Empty<AttendanceException>()
        };
    }

    private static AttendanceCalculationParameters DefaultParameters()
    {
        return new AttendanceCalculationParameters
        {
            AllowAfterOT = false,
            AllowEarlyOT = false,
            IntervalOfAfterOT = 30,
            IntervalOfAfterOTAlternate = 30,
            IntervalOfEarlyOT = 30,
            IntervalOfEarlyOTAlternate = 30,
            LimitAfterMaxOT = false,
            AfterMaxOT = 72,
            LimitEarlyMaxOT = false,
            EarlyMaxOT = 69,
            NoInAbsent = 0,
            MinsNoIn = 54,
            NoOutAbsent = 0,
            MinsNoLeave = 44,
            EarlyAbsent = false,
            MinsEarlyAbsent = 42,
            LateAbsent = false,
            MinsLateAbsent = 38,
            ShowNoTurn = true,
            AllowNoTurnOT = true,
            LimitNoTurnOT = 0,
            ShowHoliday = true,
            AllowHolidayOT = true,
            LimitHolidayOT = 0,
            WeekenFullDayOT = true,
            Weekends = 0
        };
    }

    private static AttendanceMark Mark(DateTime timestamp, string? checkType = "I")
    {
        return new AttendanceMark
        {
            PersonId = 1,
            Timestamp = timestamp,
            CheckType = checkType,
            IsPreviousDayClosureMark = string.Equals(checkType, "L", StringComparison.OrdinalIgnoreCase),
            MarkType = string.Equals(checkType, "L", StringComparison.OrdinalIgnoreCase)
                ? AttendanceMarkType.PreviousDayClosure
                : AttendanceMarkType.CheckIn
        };
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}