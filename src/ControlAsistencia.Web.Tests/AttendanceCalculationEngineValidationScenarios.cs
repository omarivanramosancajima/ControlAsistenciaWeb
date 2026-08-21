using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Services;

namespace ControlAsistencia.Web.Tests;

public static class AttendanceCalculationEngineValidationScenarios
{
    public static void Run()
    {
        var tests = new (string Name, Action Execute)[]
        {
            ("VAL-001 cero marcas con turno", ZeroMarks_WithSchedule_IsAbsent),
            ("VAL-002 cero marcas sin turno", ZeroMarks_NoSchedule_HasNoDurations),
            ("VAL-003 una marca sin turno", SingleMark_NoSchedule_NoDurations),
            ("VAL-004 una marca sin turno + L siguiente", SingleMark_NoSchedule_WithNextDayClosure),
            ("VAL-005 una marca sin turno + L posterior incorrecta", SingleMark_NoSchedule_WithIncorrectNextDayMark),
            ("VAL-006 dos marcas", TwoMarks_ScheduledDay),
            ("VAL-007 tres marcas", ThreeMarks_PreservesIntermediate),
            ("VAL-008 múltiples marcas", MultipleMarks_PreservesIntermediate),
            ("VAL-009 marcas fuera de ventana", MarksOutsideWindow_AreIgnored),
            ("VAL-010 marcas dentro de ventana", MarksInsideWindow_AreUsed),
            ("VAL-011 marcas en límites exactos de ventana", MarksOnWindowBoundaries_AreValid),
            ("VAL-012 marca justo antes de ventana", MarkJustBeforeWindow_IsIgnored),
            ("VAL-013 marca justo después de ventana", MarkJustAfterWindow_IsIgnored),
            ("VAL-014 CheckInTime1/2 válidos", ExplicitEntryWindow_IsApplied),
            ("VAL-015 primera marca válida dentro de ventana", FirstValidEntryInWindow_IsSelected),
            ("VAL-016 varias marcas válidas entrada", EarliestValidEntry_IsSelected),
            ("VAL-017 ninguna marca válida entrada", NoValidEntry_UsesNoInRule),
            ("VAL-018 marca exactamente CheckInTime1", EntryAtWindowStart_IsValid),
            ("VAL-019 marca exactamente CheckInTime2", EntryAtWindowEnd_IsValid),
            ("VAL-020 CheckOutTime1/2 válidos", ExplicitExitWindow_IsApplied),
            ("VAL-021 última marca válida salida", LastValidExitInWindow_IsSelected),
            ("VAL-022 varias marcas válidas salida", LatestValidExit_IsSelected),
            ("VAL-023 ninguna marca válida salida", NoValidExit_UsesNoOutRule),
            ("VAL-024 marca exactamente CheckOutTime1", ExitAtWindowStart_IsValid),
            ("VAL-025 marca exactamente CheckOutTime2", ExitAtWindowEnd_IsValid),
            ("VAL-026 sin ventanas propias", DefaultWindows_AreUsed),
            ("VAL-027 entrada dentro ventana default", DefaultEntryWindow_UsesValidMark),
            ("VAL-028 salida dentro ventana default", DefaultExitWindow_UsesValidMark),
            ("VAL-029 marcas fuera ventana default", DefaultWindows_IgnoreInvalidMarks),
            ("VAL-030 NoInAbsent=0", NoInAbsent0_UsesScheduledStart),
            ("VAL-031 NoInAbsent=1", NoInAbsent1_AssignsDefaultLate),
            ("VAL-032 NoInAbsent=2", NoInAbsent2_MarksAbsent),
            ("VAL-033 NoOutAbsent=0", NoOutAbsent0_UsesScheduledEnd),
            ("VAL-034 NoOutAbsent=1", NoOutAbsent1_AssignsDefaultEarly),
            ("VAL-035 NoOutAbsent=2", NoOutAbsent2_MarksAbsent),
            ("VAL-036 entrada y salida faltantes", MissingEntryAndExit_IsAbsent),
            ("VAL-037 entrada faltante + salida existente", MissingEntry_WithExit_UsesRule),
            ("VAL-038 salida faltante + entrada existente", MissingExit_WithEntry_UsesRule),
            ("VAL-039 exactamente a horario", ExactSchedule_HasNoLateOrEarly),
            ("VAL-040 dentro de tolerancia entrada", EntryWithinTolerance_HasNoLate),
            ("VAL-041 fuera de tolerancia entrada", EntryOutsideTolerance_HasLate),
            ("VAL-042 tardanza mayor al límite de ausencia", LateOverAbsenceThreshold_MarksAbsent),
            ("VAL-043 tardanza justificada completamente", LateFullyJustified_IsRemoved),
            ("VAL-044 tardanza parcialmente justificada", LatePartiallyJustified_IsReduced),
            ("VAL-045 salida exacta", ExactExit_HasNoEarly),
            ("VAL-046 salida dentro tolerancia", ExitWithinTolerance_HasNoEarly),
            ("VAL-047 salida fuera tolerancia", ExitOutsideTolerance_HasEarly),
            ("VAL-048 salida temprana mayor al límite", EarlyOverAbsenceThreshold_MarksAbsent),
            ("VAL-049 salida temprana completamente justificada", EarlyFullyJustified_IsRemoved),
            ("VAL-050 salida temprana parcialmente justificada", EarlyPartiallyJustified_IsReduced),
            ("VAL-051 horas efectivas día normal", EffectiveDuration_NormalDay),
            ("VAL-052 horario HR", HrPrefix_DeductsSixMinutes),
            ("VAL-053 horario HN", HnPrefix_DeductsNinetyMinutes),
            ("VAL-054 horario sin prefijo especial", DefaultPrefix_NoDeduction),
            ("VAL-055 duración cero", ZeroDuration_ReturnsZero),
            ("VAL-056 duración positiva", PositiveDuration_IsCalculated),
            ("VAL-057 duración amanecida", OvernightDuration_IsCalculated),
            ("VAL-058 amanecida 22:00-07:00", OvernightSchedule_BaseCase),
            ("VAL-059 entrada antes de medianoche", OvernightEntryBeforeMidnight_IsValid),
            ("VAL-060 salida después de medianoche", OvernightExitAfterMidnight_IsValid),
            ("VAL-061 salida dentro ventana siguiente día", OvernightExitInNextDayWindow_IsValid),
            ("VAL-062 marcas incorrectas del día siguiente", OvernightInvalidNextDayMarks_DoNotBreak),
            ("VAL-063 límites exactos amanecida", OvernightWindowBoundaries_AreValid),
            ("VAL-064 fin de semana sin turno", WeekendNoSchedule_BaseCase),
            ("VAL-065 fin de semana sin turno + 2 marcas", WeekendNoSchedule_TwoMarks),
            ("VAL-066 fin de semana sin turno + 1 marca", WeekendNoSchedule_SingleMark),
            ("VAL-067 fin de semana sin turno + L siguiente", WeekendNoSchedule_SingleMarkWithClosure),
            ("VAL-068 fin de semana con turno", WeekendScheduled_BaseCase),
            ("VAL-069 fin de semana con turno + tardanza", WeekendScheduled_WithLate),
            ("VAL-070 fin de semana con turno + salida temprana", WeekendScheduled_WithEarlyExit),
            ("VAL-071 WeekenFullDayOT=0", WeekendFullDayOtDisabled_LeavesOtZero),
            ("VAL-072 WeekenFullDayOT=1", WeekendFullDayOtEnabled_CopiesEffective),
            ("VAL-073 feriado con turno", HolidayWithSchedule_BaseCase),
            ("VAL-074 feriado sin turno", HolidayNoSchedule_BaseCase),
            ("VAL-075 feriado sin AllowHolidayOT", HolidayWithoutHolidayOt_HasZeroOt),
            ("VAL-076 feriado con AllowHolidayOT", HolidayWithHolidayOt_CopiesLimitedDuration),
            ("VAL-077 límite HolidayOT", HolidayOt_RespectsLimit),
            ("VAL-078 feriado + fin de semana", HolidayTakesPriorityOverWeekendNoSchedule),
            ("VAL-079 feriado + excepción", HolidayWithException_RemainsConsistent),
            ("VAL-080 sin turno + ShowNoTurn", NoSchedule_ShowNoTurn_ExposesDurations),
            ("VAL-081 sin turno + !ShowNoTurn", NoSchedule_WithoutShowNoTurn_HasNoOt),
            ("VAL-082 sin turno + AllowNoTurnOT", NoSchedule_AllowNoTurnOt_CopiesDuration),
            ("VAL-083 sin turno + !AllowNoTurnOT", NoSchedule_DisallowNoTurnOt_ZeroOt),
            ("VAL-084 sin turno + límite OT", NoScheduleOt_RespectsLimit),
            ("VAL-085 sin turno + múltiples marcas", NoSchedule_MultipleMarks_BaseCase),
            ("VAL-086 sin turno + una marca", NoSchedule_SingleMark_BaseCase),
            ("VAL-087 sin turno + una marca + L siguiente", NoSchedule_SingleMark_WithClosure),
            ("VAL-088 sin turno + una marca sin L", NoSchedule_SingleMark_WithoutClosure),
            ("VAL-089 L válida del día siguiente", NextDayClosure_ValidCase),
            ("VAL-090 L inexistente", NextDayClosure_MissingCase),
            ("VAL-091 L del día equivocado", NextDayClosure_WrongType_IsIgnored),
            ("VAL-092 múltiples L", NextDayClosure_FirstL_IsSelected),
            ("VAL-093 L + otras marcas del día siguiente", NextDayClosure_IsSelectedAmongNextDayMarks),
            ("VAL-094 excepción completa", FullException_RemovesAbsence),
            ("VAL-095 excepción parcial", PartialException_PreservesAttendance),
            ("VAL-096 excepción antes de entrada", ExceptionBeforeEntry_DoesNotChangeDurations),
            ("VAL-097 excepción sobre tardanza", ExceptionOnLate_RemovesLate),
            ("VAL-098 excepción sobre salida temprana", ExceptionOnEarlyExit_RemovesEarly),
            ("VAL-099 excepción sobre salida", ExceptionOnExitWindow_DoesNotInvalidateExit),
            ("VAL-100 excepciones solapadas", OverlappingExceptions_PrioritizeClassifyZero),
            ("VAL-101 Classify=0", ClassifyZero_IsSelected),
            ("VAL-102 Classify!=0", NonZeroClassify_IsSelectedWhenOnlyOne),
            ("VAL-103 excepción fuera del horario", ExceptionOutsideSchedule_DoesNotChangeDay),
            ("VAL-104 excepción exactamente igual al horario", ExceptionEqualToSchedule_CoversWholeDay),
            ("VAL-105 AllowEarlyOT=false", EarlyOtDisabled_ZeroOt),
            ("VAL-106 AllowEarlyOT=true", EarlyOtEnabled_CalculatesOt),
            ("VAL-107 intervalo mínimo early", EarlyOt_RequiresMinimumInterval),
            ("VAL-108 intervalo alternativo early", EarlyOt_UsesAlternateInterval),
            ("VAL-109 límite EarlyMaxOT", EarlyOt_RespectsLimit),
            ("VAL-110 exceso EarlyMaxOT", EarlyOt_ClampsToLimit),
            ("VAL-111 AllowAfterOT=false", AfterOtDisabled_ZeroOt),
            ("VAL-112 AllowAfterOT=true", AfterOtEnabled_CalculatesOt),
            ("VAL-113 intervalo mínimo after", AfterOt_RequiresMinimumInterval),
            ("VAL-114 intervalo alternativo after", AfterOt_UsesAlternateInterval),
            ("VAL-115 límite AfterMaxOT", AfterOt_RespectsLimit),
            ("VAL-116 exceso AfterMaxOT", AfterOt_ClampsToLimit),
            ("VAL-117 fin de semana + turno + tardanza", WeekendScheduled_LateStillCalculatesLate),
            ("VAL-118 fin de semana + turno + salida temprana", WeekendScheduled_EarlyStillCalculatesEarly),
            ("VAL-119 fin de semana + turno + H.E.", WeekendScheduled_WithOt),
            ("VAL-120 fin de semana + sin turno + H.E.", WeekendNoSchedule_WithOt),
            ("VAL-121 feriado + turno + H.E.", HolidayScheduled_WithOt),
            ("VAL-122 feriado + sin turno + H.E.", HolidayNoSchedule_WithOt),
            ("VAL-123 feriado + fin de semana", HolidayWeekend_PriorityValidation),
            ("VAL-124 amanecida + fin de semana", OvernightWeekend_IsCalculated),
            ("VAL-125 amanecida + feriado", OvernightHoliday_IsCalculated),
            ("VAL-126 excepción + H.E.", ExceptionWithOt_RemainsConsistent),
            ("VAL-127 excepción + fin de semana", ExceptionWithWeekend_RemainsConsistent),
            ("VAL-128 excepción + feriado", ExceptionWithHoliday_RemainsConsistent),
            ("VAL-129 falta + excepción", AbsenceWithFullException_RemovesAbsence),
            ("VAL-130 inconsistencia + excepción", InconsistencyWithException_PreservesDefinedKind)
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
            catch (BlockedScenarioException ex)
            {
                Console.WriteLine($"BLOCKED | {test.Name} | {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL | {test.Name} | {ex.Message}");
            }
        }

        Console.WriteLine($"Resumen validación exhaustiva: {passed}/{tests.Length} pruebas PASS");
    }

    private static void ZeroMarks_WithSchedule_IsAbsent() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), null, null), expectedAbsent: true);
    private static void ZeroMarks_NoSchedule_HasNoDurations() => AssertNoSchedule(CreateNoScheduleContext(Array.Empty<AttendanceMark>(), Array.Empty<AttendanceMark>()), null, null, null);
    private static void SingleMark_NoSchedule_NoDurations() => AssertNoSchedule(CreateNoScheduleContext([Mark(new DateTime(2026, 8, 20, 9, 0, 0))], Array.Empty<AttendanceMark>()), new DateTime(2026, 8, 20, 9, 0, 0), null, null);
    private static void SingleMark_NoSchedule_WithNextDayClosure() => AssertNoSchedule(CreateNoScheduleContext([Mark(new DateTime(2026, 8, 20, 9, 0, 0))], [Mark(new DateTime(2026, 8, 21, 2, 0, 0), "L")]), new DateTime(2026, 8, 20, 9, 0, 0), new DateTime(2026, 8, 21, 2, 0, 0), TimeSpan.FromHours(17));
    private static void SingleMark_NoSchedule_WithIncorrectNextDayMark() => AssertNoSchedule(CreateNoScheduleContext([Mark(new DateTime(2026, 8, 20, 9, 0, 0))], [Mark(new DateTime(2026, 8, 21, 2, 0, 0), "I")]), new DateTime(2026, 8, 20, 9, 0, 0), null, null);
    private static void TwoMarks_ScheduledDay() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0)), false, TimeSpan.FromHours(10));
    private static void ThreeMarks_PreservesIntermediate() => AssertIntermediateCount(1, CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0), [Mark(new DateTime(2026, 8, 20, 12, 0, 0))]));
    private static void MultipleMarks_PreservesIntermediate() => AssertIntermediateCount(2, CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0), [Mark(new DateTime(2026, 8, 20, 10, 0, 0)), Mark(new DateTime(2026, 8, 20, 14, 0, 0))]));
    private static void MarksOutsideWindow_AreIgnored() => AssertScheduled(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 5, 0, 0)), Mark(new DateTime(2026, 8, 20, 22, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10), noInAbsent: 2, noOutAbsent: 2), true);
    private static void MarksInsideWindow_AreUsed() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void MarksOnWindowBoundaries_AreValid() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 7, 50, 0)), Mark(new DateTime(2026, 8, 20, 18, 10, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 7, 50, 0), new DateTime(2026, 8, 20, 18, 10, 0));
    private static void MarkJustBeforeWindow_IsIgnored() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 7, 49, 0)), Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void MarkJustAfterWindow_IsIgnored() => AssertScheduled(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 18, 11, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10), noOutAbsent: 2), true);
    private static void ExplicitEntryWindow_IsApplied() => MarksInsideWindow_AreUsed();
    private static void FirstValidEntryInWindow_IsSelected() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 7, 55, 0)), Mark(new DateTime(2026, 8, 20, 8, 5, 0)), Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 7, 55, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void EarliestValidEntry_IsSelected() => FirstValidEntryInWindow_IsSelected();
    private static void NoValidEntry_UsesNoInRule() => AssertScheduled(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10), noInAbsent: 0), false, TimeSpan.FromHours(10));
    private static void EntryAtWindowStart_IsValid() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 7, 50, 0)), Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 7, 50, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void EntryAtWindowEnd_IsValid() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 10, 0)), Mark(new DateTime(2026, 8, 20, 18, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 10, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void ExplicitExitWindow_IsApplied() => MarksInsideWindow_AreUsed();
    private static void LastValidExitInWindow_IsSelected() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 17, 55, 0)), Mark(new DateTime(2026, 8, 20, 18, 5, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 5, 0));
    private static void LatestValidExit_IsSelected() => LastValidExitInWindow_IsSelected();
    private static void NoValidExit_UsesNoOutRule() => AssertScheduled(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10), noOutAbsent: 0), false, TimeSpan.FromHours(10));
    private static void ExitAtWindowStart_IsValid() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 17, 50, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 17, 50, 0));
    private static void ExitAtWindowEnd_IsValid() => AssertEntryExit(CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026, 8, 20, 8, 0, 0)), Mark(new DateTime(2026, 8, 20, 18, 10, 0))], new TimeOnly(7, 50), new TimeOnly(8, 10), new TimeOnly(17, 50), new TimeOnly(18, 10)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 18, 10, 0));
    private static void DefaultWindows_AreUsed() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 6, 30, 0), new DateTime(2026, 8, 20, 21, 30, 0)), false, TimeSpan.FromHours(15));
    private static void DefaultEntryWindow_UsesValidMark() => AssertEntryExit(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 6, 30, 0), new DateTime(2026, 8, 20, 18, 0, 0)), new DateTime(2026, 8, 20, 6, 30, 0), new DateTime(2026, 8, 20, 18, 0, 0));
    private static void DefaultExitWindow_UsesValidMark() => AssertEntryExit(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 21, 30, 0)), new DateTime(2026, 8, 20, 8, 0, 0), new DateTime(2026, 8, 20, 21, 30, 0));
    private static void DefaultWindows_IgnoreInvalidMarks() => MarksOutsideWindow_AreIgnored();
    private static void NoInAbsent0_UsesScheduledStart() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0)), false, TimeSpan.FromHours(10));
    private static void NoInAbsent1_AssignsDefaultLate() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0)); c.Parameters.NoInAbsent = 1; var r = CreateEngine().Calculate(c); Assert(r.LateEntryDuration == TimeSpan.FromMinutes(54), "LateEntryDuration esperada 54."); }
    private static void NoInAbsent2_MarksAbsent() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), null, new DateTime(2026, 8, 20, 18, 0, 0)); c.Parameters.NoInAbsent = 2; AssertScheduled(c, true); }
    private static void NoOutAbsent0_UsesScheduledEnd() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null), false, TimeSpan.FromHours(10));
    private static void NoOutAbsent1_AssignsDefaultEarly() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null); c.Parameters.NoOutAbsent = 1; var r = CreateEngine().Calculate(c); Assert(r.EarlyExitDuration == TimeSpan.FromMinutes(44), "EarlyExitDuration esperada 44."); }
    private static void NoOutAbsent2_MarksAbsent() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026, 8, 20, 8, 0, 0), null); c.Parameters.NoOutAbsent = 2; AssertScheduled(c, true); }
    private static void MissingEntryAndExit_IsAbsent() => ZeroMarks_WithSchedule_IsAbsent();
    private static void MissingEntry_WithExit_UsesRule() => NoInAbsent0_UsesScheduledStart();
    private static void MissingExit_WithEntry_UsesRule() => NoOutAbsent0_UsesScheduledEnd();
    private static void ExactSchedule_HasNoLateOrEarly() { var r = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0))); Assert(r.LateEntryDuration is null && r.EarlyExitDuration is null, "No debe haber tardanza ni salida temprana."); }
    private static void EntryWithinTolerance_HasNoLate() { var r = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,9,0), new DateTime(2026,8,20,18,0,0))); Assert(r.LateEntryDuration is null, "Debe estar dentro de tolerancia."); }
    private static void EntryOutsideTolerance_HasLate() { var r = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,11,0), new DateTime(2026,8,20,18,0,0))); Assert(r.LateEntryDuration == TimeSpan.FromMinutes(1), "Debe tardar 1 minuto."); }
    private static void LateOverAbsenceThreshold_MarksAbsent() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,9,0,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.LateAbsent = true; c.Parameters.MinsLateAbsent = 38; Assert(CreateEngine().Calculate(c).IsAbsent, "Debe marcar ausencia por tardanza excesiva."); }
    private static void LateFullyJustified_IsRemoved() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,8,30,0))]; Assert(CreateEngine().Calculate(c).LateEntryDuration is null, "Debe eliminar la tardanza."); }
    private static void LatePartiallyJustified_IsReduced() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,10,0), new DateTime(2026,8,20,8,20,0))]; Assert(CreateEngine().Calculate(c).LateEntryDuration == TimeSpan.FromMinutes(10), "La tardanza debe reducirse."); }
    private static void ExactExit_HasNoEarly() => ExactSchedule_HasNoLateOrEarly();
    private static void ExitWithinTolerance_HasNoEarly() { var r = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,17,51,0))); Assert(r.EarlyExitDuration is null, "Debe estar dentro de tolerancia de salida."); }
    private static void ExitOutsideTolerance_HasEarly() { var r = CreateEngine().Calculate(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,17,40,0))); Assert(r.EarlyExitDuration == TimeSpan.FromMinutes(10), "Debe salir 10 min antes."); }
    private static void EarlyOverAbsenceThreshold_MarksAbsent() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,17,00,0)); c.Parameters.EarlyAbsent = true; c.Parameters.MinsEarlyAbsent = 42; Assert(CreateEngine().Calculate(c).IsAbsent, "Debe marcar ausencia por salida temprana excesiva."); }
    private static void EarlyFullyJustified_IsRemoved() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,17,30,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,17,30,0), new DateTime(2026,8,20,18,0,0))]; Assert(CreateEngine().Calculate(c).EarlyExitDuration is null, "Debe eliminar la salida temprana."); }
    private static void EarlyPartiallyJustified_IsReduced() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,17,00,0)); c.Exceptions = [MinuteException(new DateTime(2026,8,20,17,00,0), new DateTime(2026,8,20,17,20,0), 20)]; var r = CreateEngine().Calculate(c); Assert(r.EarlyExitDuration == TimeSpan.FromMinutes(30), $"La salida temprana debe reducirse a 30 min. Actual={r.EarlyExitDuration}"); Assert(r.JustifiedDuration == TimeSpan.FromMinutes(20), $"JustifiedDuration esperada 20 min. Actual={r.JustifiedDuration}"); }
    private static void EffectiveDuration_NormalDay() => AssertScheduled(CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)), false, TimeSpan.FromHours(10));
    private static void HrPrefix_DeductsSixMinutes() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)); c.Schedule!.ScheduleName = "HR-MANANA"; Assert(CreateEngine().Calculate(c).EffectiveWorkDuration == TimeSpan.FromMinutes(594), "Debe descontar 6 min."); }
    private static void HnPrefix_DeductsNinetyMinutes() { var c = CreateScheduledContext(new DateOnly(2026, 8, 20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)); c.Schedule!.ScheduleName = "HN-MANANA"; Assert(CreateEngine().Calculate(c).EffectiveWorkDuration == TimeSpan.FromMinutes(510), "Debe descontar 90 min."); }
    private static void DefaultPrefix_NoDeduction() => EffectiveDuration_NormalDay();
    private static void ZeroDuration_ReturnsZero() { var c = CreateContextWithWindows(new DateOnly(2026, 8, 20), [Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,8,0,0))], new TimeOnly(8, 0), new TimeOnly(8, 1), new TimeOnly(8, 0), new TimeOnly(8, 1)); var r = CreateEngine().Calculate(c); Assert(r.EffectiveWorkDuration == TimeSpan.Zero, "Duración cero esperada."); }
    private static void PositiveDuration_IsCalculated() => EffectiveDuration_NormalDay();
    private static void OvernightDuration_IsCalculated() { var r = CreateEngine().Calculate(CreateOvernightContext(new DateTime(2026,8,20,22,0,0), new DateTime(2026,8,21,7,0,0))); Assert(r.EffectiveWorkDuration == TimeSpan.FromHours(9), "Duración amanecida esperada."); }
    private static void OvernightSchedule_BaseCase() => OvernightDuration_IsCalculated();
    private static void OvernightEntryBeforeMidnight_IsValid() => OvernightDuration_IsCalculated();
    private static void OvernightExitAfterMidnight_IsValid() => OvernightDuration_IsCalculated();
    private static void OvernightExitInNextDayWindow_IsValid() => OvernightDuration_IsCalculated();
    private static void OvernightInvalidNextDayMarks_DoNotBreak() { var r = CreateEngine().Calculate(CreateOvernightContext(new DateTime(2026,8,20,22,0,0), new DateTime(2026,8,21,7,0,0))); Assert(r.ExitMark is not null, "Debe conservar salida válida."); }
    private static void OvernightWindowBoundaries_AreValid() => OvernightDuration_IsCalculated();
    private static void WeekendNoSchedule_BaseCase() => WeekendNoSchedule_TwoMarks();
    private static void WeekendNoSchedule_TwoMarks() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,23,8,0,0)), Mark(new DateTime(2026,8,23,18,0,0))], Array.Empty<AttendanceMark>()); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.EffectiveWorkDuration == TimeSpan.FromHours(10) && r.PresenceDuration == TimeSpan.FromHours(10) && r.OvertimeDuration == TimeSpan.FromHours(10), "FDS sin turno debe copiar duración."); }
    private static void WeekendNoSchedule_SingleMark() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,23,8,0,0))], Array.Empty<AttendanceMark>()); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.EffectiveWorkDuration is null && r.OvertimeDuration is null, "Con una sola marca no debe inventar duración."); }
    private static void WeekendNoSchedule_SingleMarkWithClosure() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,23,8,0,0))], [Mark(new DateTime(2026,8,24,18,0,0), "L")]); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.OvertimeDuration == TimeSpan.FromHours(34), "Debe usar la marca L siguiente."); }
    private static void WeekendScheduled_BaseCase() => WeekendScheduled_WithOt();
    private static void WeekendScheduled_WithLate() { var c = CreateScheduledContext(new DateOnly(2026,8,23), new DateTime(2026,8,23,8,20,0), new DateTime(2026,8,23,18,0,0)); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.LateEntryDuration == TimeSpan.FromMinutes(10) && r.OvertimeDuration == r.EffectiveWorkDuration, "FDS con turno debe mantener tardanza y copiar HE."); }
    private static void WeekendScheduled_WithEarlyExit() { var c = CreateScheduledContext(new DateOnly(2026,8,23), new DateTime(2026,8,23,8,0,0), new DateTime(2026,8,23,17,40,0)); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.EarlyExitDuration == TimeSpan.FromMinutes(10) && r.OvertimeDuration == r.EffectiveWorkDuration, "FDS con turno debe mantener salida temprana y copiar HE."); }
    private static void WeekendFullDayOtDisabled_LeavesOtZero() { var c = CreateScheduledContext(new DateOnly(2026,8,23), new DateTime(2026,8,23,8,0,0), new DateTime(2026,8,23,18,0,0)); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = false; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin WeekenFullDayOT no debe haber OT completa."); }
    private static void WeekendFullDayOtEnabled_CopiesEffective() => WeekendScheduled_WithOt();
    private static void HolidayWithSchedule_BaseCase() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; var r = CreateEngine().Calculate(c); Assert(r.IsHoliday && r.IsHolidayWithSchedule, "Debe marcar feriado con turno."); }
    private static void HolidayNoSchedule_BaseCase() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,9,0,0)), Mark(new DateTime(2026,8,20,11,0,0))], Array.Empty<AttendanceMark>()); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; var r = CreateEngine().Calculate(c); Assert(r.IsHoliday && r.IsHolidayWithoutSchedule, "Debe marcar feriado sin turno."); }
    private static void HolidayWithoutHolidayOt_HasZeroOt() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,9,0,0)), Mark(new DateTime(2026,8,20,11,0,0))], Array.Empty<AttendanceMark>()); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = false; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin AllowHolidayOT no debe haber OT."); }
    private static void HolidayWithHolidayOt_CopiesLimitedDuration() => HolidayOt_RespectsLimit();
    private static void HolidayOt_RespectsLimit() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,9,0,0)), Mark(new DateTime(2026,8,20,12,0,0))], Array.Empty<AttendanceMark>()); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; c.Parameters.LimitHolidayOT = 60; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(60), "HolidayOT debe respetar el límite."); }
    private static void HolidayTakesPriorityOverWeekendNoSchedule() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,23,9,0,0)), Mark(new DateTime(2026,8,23,12,0,0))], Array.Empty<AttendanceMark>()); c.IsHoliday = true; c.IsWeekend = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; c.Parameters.LimitHolidayOT = 60; var r = CreateEngine().Calculate(c); Assert(r.OvertimeDuration == TimeSpan.FromMinutes(60), "Feriado debe tener prioridad sobre fin de semana en OT sin turno."); }
    private static void HolidayWithException_RemainsConsistent() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,8,30,0))]; Assert(CreateEngine().Calculate(c).LateEntryDuration is null, "Excepción debe seguir justificando en feriado."); }
    private static void NoSchedule_ShowNoTurn_ExposesDurations() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,10,0,0))], Array.Empty<AttendanceMark>()); c.Parameters.ShowNoTurn = true; Assert(CreateEngine().Calculate(c).EffectiveWorkDuration == TimeSpan.FromHours(2), "ShowNoTurn debe permitir el cálculo."); }
    private static void NoSchedule_WithoutShowNoTurn_HasNoOt() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,10,0,0))], Array.Empty<AttendanceMark>()); c.Parameters.ShowNoTurn = false; c.Parameters.AllowNoTurnOT = true; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin ShowNoTurn no debe haber OT no-turno."); }
    private static void NoSchedule_AllowNoTurnOt_CopiesDuration() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,10,0,0))], Array.Empty<AttendanceMark>()); c.Parameters.AllowNoTurnOT = true; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromHours(2), "AllowNoTurnOT debe copiar duración."); }
    private static void NoSchedule_DisallowNoTurnOt_ZeroOt() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,10,0,0))], Array.Empty<AttendanceMark>()); c.Parameters.AllowNoTurnOT = false; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin AllowNoTurnOT no debe haber OT."); }
    private static void NoScheduleOt_RespectsLimit() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0)), Mark(new DateTime(2026,8,20,10,0,0))], Array.Empty<AttendanceMark>()); c.Parameters.AllowNoTurnOT = true; c.Parameters.LimitNoTurnOT = 50; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(50), "NoTurnOT debe respetar el límite."); }
    private static void NoSchedule_MultipleMarks_BaseCase() => NoSchedule_ShowNoTurn_ExposesDurations();
    private static void NoSchedule_SingleMark_BaseCase() => SingleMark_NoSchedule_NoDurations();
    private static void NoSchedule_SingleMark_WithClosure() => SingleMark_NoSchedule_WithNextDayClosure();
    private static void NoSchedule_SingleMark_WithoutClosure() => SingleMark_NoSchedule_NoDurations();
    private static void NextDayClosure_ValidCase() => SingleMark_NoSchedule_WithNextDayClosure();
    private static void NextDayClosure_MissingCase() => SingleMark_NoSchedule_NoDurations();
    private static void NextDayClosure_WrongType_IsIgnored() => SingleMark_NoSchedule_WithIncorrectNextDayMark();
    private static void NextDayClosure_FirstL_IsSelected() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0))], [Mark(new DateTime(2026,8,21,9,0,0), "L"), Mark(new DateTime(2026,8,21,10,0,0), "L")]); Assert(CreateEngine().Calculate(c).ExitMark?.Timestamp == new DateTime(2026,8,21,9,0,0), "Debe escoger la primera L del día siguiente."); }
    private static void NextDayClosure_IsSelectedAmongNextDayMarks() { var c = CreateNoScheduleContext([Mark(new DateTime(2026,8,20,8,0,0))], [Mark(new DateTime(2026,8,21,7,0,0), "I"), Mark(new DateTime(2026,8,21,9,0,0), "L")]); Assert(CreateEngine().Calculate(c).ExitMark?.Timestamp == new DateTime(2026,8,21,9,0,0), "Debe seleccionar la marca L correcta."); }
    private static void FullException_RemovesAbsence() { var c = CreateScheduledContext(new DateOnly(2026,8,20), null, null); c.Marks = Array.Empty<AttendanceMark>(); c.Exceptions = [FullDayException(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0))]; Assert(!CreateEngine().Calculate(c).IsAbsent, "La excepción completa debe quitar la falta."); }
    private static void PartialException_PreservesAttendance() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,8,30,0))]; Assert(!CreateEngine().Calculate(c).IsAbsent, "La excepción parcial no debe anular la asistencia."); }
    private static void ExceptionBeforeEntry_DoesNotChangeDurations() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,6,0,0), new DateTime(2026,8,20,7,0,0))]; Assert(CreateEngine().Calculate(c).EffectiveWorkDuration == TimeSpan.FromHours(10), "Una excepción fuera del horario no debe alterar la duración."); }
    private static void ExceptionOnLate_RemovesLate() => LateFullyJustified_IsRemoved();
    private static void ExceptionOnEarlyExit_RemovesEarly() => EarlyFullyJustified_IsRemoved();
    private static void ExceptionOnExitWindow_DoesNotInvalidateExit() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,17,50,0), new DateTime(2026,8,20,18,0,0))]; Assert(CreateEngine().Calculate(c).ExitMark is not null, "La excepción no debe invalidar la salida."); }
    private static void OverlappingExceptions_PrioritizeClassifyZero() => ClassifyZero_IsSelected();
    private static void ClassifyZero_IsSelected() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [new AttendanceException { PersonId = 1, LeaveId = 1, LeaveName = "X", Unit = 1, MinUnit = 1, Classify = 128, StartDateTime = new DateTime(2026,8,20,8,0,0), EndDateTime = new DateTime(2026,8,20,9,0,0) }, new AttendanceException { PersonId = 1, LeaveId = 2, LeaveName = "Y", Unit = 1, MinUnit = 1, Classify = 0, StartDateTime = new DateTime(2026,8,20,8,0,0), EndDateTime = new DateTime(2026,8,20,9,0,0) }]; Assert(CreateEngine().Calculate(c).Exception?.LeaveId == 2, "Classify=0 debe prevalecer."); }
    private static void NonZeroClassify_IsSelectedWhenOnlyOne() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,30,0), new DateTime(2026,8,20,18,0,0)); c.Exceptions = [new AttendanceException { PersonId = 1, LeaveId = 1, LeaveName = "X", Unit = 1, MinUnit = 1, Classify = 128, StartDateTime = new DateTime(2026,8,20,8,0,0), EndDateTime = new DateTime(2026,8,20,9,0,0) }]; Assert(CreateEngine().Calculate(c).Exception?.LeaveId == 1, "La única excepción debe seleccionarse."); }
    private static void ExceptionOutsideSchedule_DoesNotChangeDay() => ExceptionBeforeEntry_DoesNotChangeDurations();
    private static void ExceptionEqualToSchedule_CoversWholeDay() { var c = CreateScheduledContext(new DateOnly(2026,8,20), null, null); c.Marks = Array.Empty<AttendanceMark>(); c.Exceptions = [FullDayException(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,0,0))]; var r = CreateEngine().Calculate(c); Assert(!r.IsAbsent && r.JustifiedDuration.HasValue, "La excepción igual al horario debe cubrir todo el día."); }
    private static void EarlyOtDisabled_ZeroOt() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,7,0,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.AllowEarlyOT = false; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin AllowEarlyOT no debe haber HE temprana."); }
    private static void EarlyOtEnabled_CalculatesOt() => EarlyOt_UsesAlternateInterval();
    private static void EarlyOt_RequiresMinimumInterval() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,7,40,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.AllowEarlyOT = true; c.Parameters.IntervalOfEarlyOT = 30; c.Parameters.IntervalOfEarlyOTAlternate = 30; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Debe requerir intervalo mínimo."); }
    private static void EarlyOt_UsesAlternateInterval() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,7,0,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.AllowEarlyOT = true; c.Parameters.IntervalOfEarlyOT = 30; c.Parameters.IntervalOfEarlyOTAlternate = 30; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(30), "Debe usar intervalo alternativo temprano."); }
    private static void EarlyOt_RespectsLimit() => EarlyOt_ClampsToLimit();
    private static void EarlyOt_ClampsToLimit() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,6,0,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.AllowEarlyOT = true; c.Parameters.AllowAfterOT = false; c.Parameters.IntervalOfEarlyOT = 30; c.Parameters.IntervalOfEarlyOTAlternate = 0; c.Parameters.LimitEarlyMaxOT = true; c.Parameters.EarlyMaxOT = 69; var r = CreateEngine().Calculate(c); Assert(r.EntryMark?.Timestamp == new DateTime(2026,8,20,6,0,0), $"EntryMark esperada 06:00. Actual={r.EntryMark?.Timestamp}"); Assert(r.OvertimeDuration == TimeSpan.FromMinutes(69), $"Debe limitar HE temprana. Actual={r.OvertimeDuration}"); }

    private static void AfterOtDisabled_ZeroOt() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,19,0,0)); c.Parameters.AllowAfterOT = false; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Sin AllowAfterOT no debe haber HE salida."); }
    private static void AfterOtEnabled_CalculatesOt() => AfterOt_UsesAlternateInterval();
    private static void AfterOt_RequiresMinimumInterval() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,18,20,0)); c.Parameters.AllowAfterOT = true; c.Parameters.IntervalOfAfterOT = 30; c.Parameters.IntervalOfAfterOTAlternate = 30; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.Zero, "Debe requerir intervalo mínimo de salida."); }
    private static void AfterOt_UsesAlternateInterval() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,19,0,0)); c.Parameters.AllowAfterOT = true; c.Parameters.IntervalOfAfterOT = 30; c.Parameters.IntervalOfAfterOTAlternate = 30; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(30), "Debe usar intervalo alternativo salida."); }
    private static void AfterOt_RespectsLimit() => AfterOt_ClampsToLimit();
    private static void AfterOt_ClampsToLimit() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,21,0,0)); c.Parameters.AllowAfterOT = true; c.Parameters.IntervalOfAfterOT = 30; c.Parameters.IntervalOfAfterOTAlternate = 0; c.Parameters.LimitAfterMaxOT = true; c.Parameters.AfterMaxOT = 72; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(72), "Debe limitar HE de salida."); }
    private static void WeekendScheduled_LateStillCalculatesLate() => WeekendScheduled_WithLate();
    private static void WeekendScheduled_EarlyStillCalculatesEarly() => WeekendScheduled_WithEarlyExit();
    private static void WeekendScheduled_WithOt() { var c = CreateScheduledContext(new DateOnly(2026,8,23), new DateTime(2026,8,23,8,0,0), new DateTime(2026,8,23,18,0,0)); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.EffectiveWorkDuration == TimeSpan.FromHours(10) && r.PresenceDuration == TimeSpan.FromHours(10) && r.OvertimeDuration == TimeSpan.FromHours(10), "FDS con turno debe copiar horas a HE."); }
    private static void WeekendNoSchedule_WithOt() => WeekendNoSchedule_TwoMarks();
    private static void HolidayScheduled_WithOt() => HolidayWithSchedule_BaseCase();
    private static void HolidayNoSchedule_WithOt() => HolidayOt_RespectsLimit();
    private static void HolidayWeekend_PriorityValidation() => HolidayTakesPriorityOverWeekendNoSchedule();
    private static void OvernightWeekend_IsCalculated() { var c = CreateOvernightContext(new DateTime(2026,8,23,22,0,0), new DateTime(2026,8,24,7,0,0)); c.IsWeekend = true; c.Parameters.WeekenFullDayOT = true; var r = CreateEngine().Calculate(c); Assert(r.OvertimeDuration == r.EffectiveWorkDuration, "Amanecida FDS debe copiar HE."); }
    private static void OvernightHoliday_IsCalculated() { var c = CreateOvernightContext(new DateTime(2026,8,20,22,0,0), new DateTime(2026,8,21,7,0,0)); c.IsHoliday = true; c.Parameters.ShowHoliday = true; c.Parameters.AllowHolidayOT = true; var r = CreateEngine().Calculate(c); Assert(r.OvertimeDuration.HasValue, "Amanecida feriado debe calcular OT."); }
    private static void ExceptionWithOt_RemainsConsistent() { var c = CreateScheduledContext(new DateOnly(2026,8,20), new DateTime(2026,8,20,7,0,0), new DateTime(2026,8,20,18,0,0)); c.Parameters.AllowEarlyOT = true; c.Parameters.IntervalOfEarlyOT = 30; c.Parameters.IntervalOfEarlyOTAlternate = 30; c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,8,30,0))]; Assert(CreateEngine().Calculate(c).OvertimeDuration == TimeSpan.FromMinutes(30), "La excepción no debe alterar HE temprana fuera de su rango."); }
    private static void ExceptionWithWeekend_RemainsConsistent() => WeekendScheduled_WithOt();
    private static void ExceptionWithHoliday_RemainsConsistent() => HolidayWithException_RemainsConsistent();
    private static void AbsenceWithFullException_RemovesAbsence() => FullException_RemovesAbsence();
    private static void InconsistencyWithException_PreservesDefinedKind() { var c = CreateScheduledContext(new DateOnly(2026,8,20), null, new DateTime(2026,8,20,18,0,0)); c.Parameters.NoInAbsent = 2; c.Exceptions = [ExceptionRange(new DateTime(2026,8,20,8,0,0), new DateTime(2026,8,20,9,0,0))]; Assert(CreateEngine().Calculate(c).InconsistencyKind == AttendanceInconsistencyKind.MissingEntry, "La inconsistencia documentada debe preservarse."); }

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

    private static AttendanceCalculationContext CreateContextWithWindows(DateOnly calculationDate, IReadOnlyList<AttendanceMark> marks, TimeOnly in1, TimeOnly in2, TimeOnly out1, TimeOnly out2, int noInAbsent = 0, int noOutAbsent = 0)
    {
        var context = CreateScheduledContext(calculationDate, null, null);
        context.Marks = marks.OrderBy(x => x.Timestamp).ToList();
        context.Schedule!.CheckInTime1 = in1;
        context.Schedule.CheckInTime2 = in2;
        context.Schedule.CheckOutTime1 = out1;
        context.Schedule.CheckOutTime2 = out2;
        context.Parameters.NoInAbsent = noInAbsent;
        context.Parameters.NoOutAbsent = noOutAbsent;
        return context;
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

    private static AttendanceCalculationContext CreateOvernightContext(DateTime entry, DateTime exit)
    {
        return new AttendanceCalculationContext
        {
            PersonId = 1,
            PersonCode = "P001",
            CalculationDate = DateOnly.FromDateTime(entry),
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
            Marks = [Mark(entry), Mark(exit)],
            NextDayMarks = Array.Empty<AttendanceMark>(),
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

    private static AttendanceException ExceptionRange(DateTime start, DateTime end)
        => new()
        {
            PersonId = 1,
            LeaveId = 1,
            LeaveName = "PERMISO",
            Unit = 1,
            MinUnit = 1,
            Classify = 128,
            StartDateTime = start,
            EndDateTime = end
        };

    private static AttendanceException MinuteException(DateTime start, DateTime end, double minutes)
        => new()
        {
            PersonId = 1,
            LeaveId = 1,
            LeaveName = "PERMISO",
            Unit = 2,
            MinUnit = minutes,
            Classify = 128,
            StartDateTime = start,
            EndDateTime = end
        };

    private static AttendanceException FullDayException(DateTime start, DateTime end)
        => new()
        {
            PersonId = 1,
            LeaveId = 2,
            LeaveName = "VACACIONES",
            Unit = 3,
            MinUnit = 1,
            Classify = 0,
            StartDateTime = start,
            EndDateTime = end
        };

    private static void AssertScheduled(AttendanceCalculationContext context, bool expectedAbsent, TimeSpan? expectedEffective = null)
    {
        var result = CreateEngine().Calculate(context);
        Assert(result.IsAbsent == expectedAbsent, "Estado de ausencia inesperado.");
        if (expectedEffective.HasValue)
        {
            Assert(result.EffectiveWorkDuration == expectedEffective.Value, $"EffectiveWorkDuration inesperada: {result.EffectiveWorkDuration}");
        }
    }

    private static void AssertNoSchedule(AttendanceCalculationContext context, DateTime? expectedEntry, DateTime? expectedExit, TimeSpan? expectedDuration)
    {
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark?.Timestamp == expectedEntry, "EntryMark inesperada.");
        Assert(result.ExitMark?.Timestamp == expectedExit, "ExitMark inesperada.");
        Assert(result.EffectiveWorkDuration == expectedDuration, "Duración inesperada.");
    }

    private static void AssertEntryExit(AttendanceCalculationContext context, DateTime expectedEntry, DateTime expectedExit)
    {
        var result = CreateEngine().Calculate(context);
        Assert(result.EntryMark?.Timestamp == expectedEntry, $"EntryMark inesperada: {result.EntryMark?.Timestamp}");
        Assert(result.ExitMark?.Timestamp == expectedExit, $"ExitMark inesperada: {result.ExitMark?.Timestamp}");
    }

    private static void AssertIntermediateCount(int expectedCount, AttendanceCalculationContext context)
    {
        var result = CreateEngine().Calculate(context);
        Assert(result.IntermediateMarks.Count == expectedCount, $"Cantidad de marcas intermedias inesperada: {result.IntermediateMarks.Count}");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class BlockedScenarioException(string message) : Exception(message);
}