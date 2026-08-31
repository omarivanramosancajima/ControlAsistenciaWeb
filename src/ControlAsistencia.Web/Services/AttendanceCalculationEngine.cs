using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public class AttendanceCalculationEngine : IAttendanceCalculationEngine
{
    private static readonly TimeSpan DefaultEntryWindowLead = TimeSpan.FromHours(2);
    private static readonly TimeSpan DefaultExitWindowTrail = TimeSpan.FromHours(6);
    public AttendanceDayResult Calculate(AttendanceCalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var orderedMarks = context.Marks
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Source)
            .ThenBy(x => x.RecordId)
            .ToList();

        var orderedNextDayMarks = context.NextDayMarks
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Source)
            .ThenBy(x => x.RecordId)
            .ToList();

        var resolvedException = ResolveException(context);
        var result = CreateBaseResult(context);
        var accumulation = new AttendancePersonAccumulation();
        result.Exception = resolvedException.SelectedException;
        result.JustifiedDuration = resolvedException.JustifiedDuration;
        result.JustifiedDayFraction = resolvedException.JustifiedDayFraction;
        result.ExceptionDisplayText = BuildExceptionDisplayText(context, context.IsNoSchedule);
        result.ScheduleDisplayText = BuildInitialScheduleDisplayText(context);
        result.Accumulation = accumulation;

        if (context.IsNoSchedule || context.Schedule is null || !context.Schedule.HasSchedule)
        {
            CalculateNoSchedule(context, orderedMarks, orderedNextDayMarks, resolvedException, result, accumulation);
            return result;
        }

        CalculateScheduledDay(context, orderedMarks, resolvedException, result, accumulation);
        return result;
    }

    private static AttendanceDayResult CreateBaseResult(AttendanceCalculationContext context)
    {
        return new AttendanceDayResult
        {
            PersonId = context.PersonId,
            PersonCode = context.PersonCode,
            PersonDocumentNumber = context.PersonDocumentNumber,
            PersonName = context.PersonName,
            DepartmentId = context.DepartmentId,
            DepartmentName = context.DepartmentName,
            Date = context.CalculationDate,
            Schedule = context.Schedule,
            IsHoliday = context.IsHoliday,
            IsWeekend = context.IsWeekend,
            IsNoSchedule = context.IsNoSchedule,
            IsHolidayWithSchedule = context.IsHoliday && !context.IsNoSchedule,
            IsHolidayWithoutSchedule = context.IsHoliday && context.IsNoSchedule,
            InconsistencyKind = AttendanceInconsistencyKind.None
        };
    }

    private static void CalculateNoSchedule(
        AttendanceCalculationContext context,
        IReadOnlyList<AttendanceMark> orderedMarks,
        IReadOnlyList<AttendanceMark> orderedNextDayMarks,
        ExceptionResolution resolvedException,
        AttendanceDayResult result,
        AttendancePersonAccumulation accumulation)
    {
        // [ASISTWEB][SEC.02]
        if (orderedMarks.Count == 0)
        {
            // [ASISTWEB][SEC.02.00.01] Sin marcas: no procesa el día en SEC.02.
            result.ProcessedBySection02 = false;
            return;
        }

        // [ASISTWEB][SEC.02.00.02] Los flags Show* afectan visualización posterior (SEC.05), no el procesamiento del día SIN TURNO.
        result.ProcessedBySection02 = true;
        accumulation.DiasDeAsistenciaSinTurno++;

        if (context.IsHoliday)
        {
            accumulation.FeriadosSinTurno++;
        }

        ApplyNoScheduleLabels(context, result);

        if (orderedMarks.Count == 0)
        {
            result.IsAbsent = false;
            return;
        }

        if (orderedMarks.Count == 1)
        {
            result.EntryMark = orderedMarks[0];

            var nextDayClosure = orderedNextDayMarks
                .Where(x => x.IsPreviousDayClosureMark)
                .OrderBy(x => x.Timestamp)
                .FirstOrDefault();

            if (nextDayClosure is null)
            {
                // [ASISTWEB][SEC.02.01.01]
                result.IntermediateMarks = Array.Empty<AttendanceMark>();
                result.InconsistencyKind = AttendanceInconsistencyKind.SingleMarkWithoutNextDayClosure;
                result.IsAbsent = false;
                result.EffectiveWorkDuration = null;
                result.PresenceDuration = null;
                result.OvertimeDuration = null;
                ApplySingleMarkExceptionAccumulation(context, resolvedException, result, accumulation);
                accumulation.DiasDeAsistencia++;
                return;
            }

            // [ASISTWEB][SEC.02.01.02]
            result.ExitMark = nextDayClosure;
            result.IntermediateMarks = Array.Empty<AttendanceMark>();

            var rawDuration = nextDayClosure.Timestamp - orderedMarks[0].Timestamp;
            var netDuration = ApplyAutomaticBreakDeduction(context.Schedule, rawDuration);

            result.EffectiveWorkDuration = netDuration;
            result.PresenceDuration = netDuration;
            result.OvertimeDuration = ResolveNoScheduleOvertime(context, netDuration);
            result.IsAbsent = false;
            ApplyNoScheduleExceptionAccumulation(context, resolvedException, accumulation);
            AccumulateFinalDurations(result, accumulation);
            accumulation.DiasDeAsistencia++;
            return;
        }

        // [ASISTWEB][SEC.02.02]
        result.EntryMark = orderedMarks.First();
        result.ExitMark = orderedMarks.Last();
        result.IntermediateMarks = orderedMarks.Skip(1).Take(orderedMarks.Count - 2).ToList();

        var duration = result.ExitMark.Timestamp - result.EntryMark.Timestamp;
        var effective = ApplyAutomaticBreakDeduction(context.Schedule, duration);
        result.EffectiveWorkDuration = effective;
        result.PresenceDuration = effective;
        result.OvertimeDuration = ResolveNoScheduleOvertime(context, effective);
        result.IsAbsent = false;
        ApplyNoScheduleExceptionAccumulation(context, resolvedException, accumulation);
        AccumulateFinalDurations(result, accumulation);
        accumulation.DiasDeAsistencia++;
    }

    private static void CalculateScheduledDay(
        AttendanceCalculationContext context,
        IReadOnlyList<AttendanceMark> orderedMarks,
        ExceptionResolution resolvedException,
        AttendanceDayResult result,
        AttendancePersonAccumulation accumulation)
    {
        // [ASISTWEB][SEC.03]
        var schedule = context.Schedule!;
        var scheduleBounds = ResolveScheduleBounds(context.CalculationDate, schedule);
        var entryWindow = ResolveEntryWindow(scheduleBounds, schedule);
        var exitWindow = ResolveExitWindow(scheduleBounds, schedule);

        accumulation.DiasProgramadosConTurno++;
        result.ProcessedBySection03 = true;
        result.ScheduleDisplayText = BuildAssignedScheduleDisplayText(context, scheduleBounds, string.Empty);

        if (orderedMarks.Count > 0 && context.IsHoliday && !context.Parameters.ShowHoliday)
        {
            result.ProcessedBySection03 = false;
            return;
        }

        if (orderedMarks.Count > 0 && !context.IsHoliday && context.IsWeekend && !context.Parameters.ShowWeekends)
        {
            result.ProcessedBySection03 = false;
            return;
        }

        if (orderedMarks.Count == 0)
        {
            // [ASISTWEB][SEC.03.05]
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, string.Empty);
            return;
        }

        // [ASISTWEB][SEC.03.06.01] Selección literal v01.04 para días CON TURNO usando DateTime normalizados de SEC.03.03 y SEC.03.04.
        var entryMark = orderedMarks.FirstOrDefault(mark => !mark.IsPreviousDayClosureMark && IsWithinEntryWindow(mark.Timestamp, entryWindow.Start, entryWindow.End));
        var exitMark = orderedMarks.LastOrDefault(mark => !mark.IsPreviousDayClosureMark && IsWithinExitWindow(mark.Timestamp, exitWindow.Start, exitWindow.End));

        result.EntryMark = entryMark;
        result.ExitMark = exitMark;
        result.IntermediateMarks = orderedMarks
            .Where(mark => !ReferenceEquals(mark, entryMark) && !ReferenceEquals(mark, exitMark))
            .ToList();

        ScheduledScenarioState state;
        if (entryMark is null && exitMark is null)
        {
            // [ASISTWEB][SEC.03.06.02.01]
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(*)");
            return;
        }

        if (entryMark is null)
        {
            // [ASISTWEB][SEC.03.06.02.02]
            state = ProcessMissingEntryScenario(context, resolvedException, result, accumulation, scheduleBounds, exitMark!);
            if (state.IsTerminal)
            {
                return;
            }
        }
        else if (exitMark is null)
        {
            // [ASISTWEB][SEC.03.06.02.03]
            state = ProcessMissingExitScenario(context, resolvedException, result, accumulation, scheduleBounds, entryMark);
            if (state.IsTerminal)
            {
                return;
            }
        }
        else
        {
            // [ASISTWEB][SEC.03.06.02.04]
            state = ProcessEntryAndExitScenario(context, resolvedException, result, accumulation, scheduleBounds, entryMark, exitMark);
            if (state.IsTerminal)
            {
                return;
            }
        }

        // [ASISTWEB][SEC.03.06.03][SEC.03.06.04]
        result.OvertimeDuration = ResolveScheduledOvertime(
            context,
            scheduleBounds,
            state.EntryForCalculations,
            state.ExitForCalculations,
            result.EffectiveWorkDuration);

        // [ASISTWEB][SEC.03.06.05]
        ApplyScheduledExceptionRules(context, resolvedException, result, accumulation, scheduleBounds, state);

        // [ASISTWEB][SEC.03.06.06]
        result.ScheduleDisplayText = BuildAssignedScheduleDisplayText(context, scheduleBounds, string.Empty);
        result.IsAbsent = false;
        AccumulateFinalDurations(result, accumulation);

        // [ASISTWEB][SEC.03.06.07]
        if (context.IsHoliday && context.Parameters.ShowHoliday)
        {
            accumulation.FeriadosConTurno++;
        }

        accumulation.DiasDeAsistencia++;
    }

    private static TimeSpan ResolveScheduledOvertime(
        AttendanceCalculationContext context,
        ScheduleBounds scheduleBounds,
        DateTime actualEntry,
        DateTime actualExit,
        TimeSpan? effectiveWorkDuration)
    {
        var totalOt = CalculateEarlyOvertime(context, scheduleBounds, actualEntry)
            + CalculateAfterOvertime(context, scheduleBounds, actualExit);

        if (context.IsHoliday && context.Parameters.ShowHoliday && context.Parameters.AllowHolidayOT)
        {
            return totalOt > TimeSpan.Zero
                ? ApplyLimit(totalOt, context.Parameters.LimitHolidayOT)
                : TimeSpan.Zero;
        }

        if (!context.IsHoliday && context.IsWeekend)
        {
            return context.Parameters.WeekenFullDayOT
                ? (effectiveWorkDuration.HasValue && effectiveWorkDuration.Value > TimeSpan.Zero
                    ? effectiveWorkDuration.Value
                    : TimeSpan.Zero)
                : TimeSpan.Zero;
        }
        return totalOt;
    }

    private static TimeSpan CalculateAfterOvertime(AttendanceCalculationContext context, ScheduleBounds scheduleBounds, DateTime actualExit)
    {
        // [ASISTWEB][SEC.03.06.03]
        if (!context.Parameters.AllowAfterOT || context.Parameters.IntervalOfAfterOT <= 0 || actualExit <= scheduleBounds.ScheduledEnd)
        {
            return TimeSpan.Zero;
        }

        var diff = actualExit - scheduleBounds.ScheduledEnd;
        if (diff.TotalMinutes < context.Parameters.IntervalOfAfterOT)
        {
            return TimeSpan.Zero;
        }

        return context.Parameters.LimitAfterMaxOT && context.Parameters.AfterMaxOT > 0
            ? ApplyLimit(diff, context.Parameters.AfterMaxOT)
            : diff;
    }

    private static TimeSpan CalculateEarlyOvertime(AttendanceCalculationContext context, ScheduleBounds scheduleBounds, DateTime actualEntry)
    {
        // [ASISTWEB][SEC.03.06.03]
        if (!context.Parameters.AllowEarlyOT || context.Parameters.IntervalOfEarlyOT <= 0 || actualEntry >= scheduleBounds.ScheduledStart)
        {
            return TimeSpan.Zero;
        }

        var diff = scheduleBounds.ScheduledStart - actualEntry;
        if (diff.TotalMinutes < context.Parameters.IntervalOfEarlyOT)
        {
            return TimeSpan.Zero;
        }

        return context.Parameters.LimitEarlyMaxOT && context.Parameters.EarlyMaxOT > 0
            ? ApplyLimit(diff, context.Parameters.EarlyMaxOT)
            : diff;
    }

    private static TimeSpan ResolveNoScheduleOvertime(AttendanceCalculationContext context, TimeSpan effectiveDuration)
    {
        if (context.IsHoliday)
        {
            if (!context.Parameters.AllowHolidayOT)
            {
                return TimeSpan.Zero;
            }

            return ApplyLimit(effectiveDuration, context.Parameters.LimitHolidayOT);
        }

        if (context.IsWeekend && context.Parameters.WeekenFullDayOT)
        {
            return effectiveDuration;
        }

        if (!context.Parameters.AllowNoTurnOT)
        {
            return TimeSpan.Zero;
        }

        return ApplyLimit(effectiveDuration, context.Parameters.LimitNoTurnOT);
    }

    private static TimeSpan ApplyLimit(TimeSpan duration, int limitMinutes)
    {
        if (limitMinutes <= 0)
        {
            return duration;
        }

        var limit = TimeSpan.FromMinutes(limitMinutes);
        return duration > limit ? limit : duration;
    }

    private static TimeSpan AdjustDurationByException(DateTime rangeStart, DateTime rangeEnd, TimeSpan current, AttendanceException? exception)
    {
        if (exception is null || current <= TimeSpan.Zero)
        {
            return current;
        }

        var overlap = GetOverlap(rangeStart, rangeEnd, exception.StartDateTime, exception.EndDateTime ?? exception.StartDateTime);
        return overlap >= current ? TimeSpan.Zero : current - overlap;
    }

    private static void ApplyFullDayExceptionIfAny(ScheduleBounds bounds, ExceptionResolution resolvedException, AttendanceDayResult result)
    {
        if (!resolvedException.CoversWholeScheduledDay)
        {
            return;
        }

        result.IsAbsent = false;
        result.LateEntryDuration = null;
        result.EarlyExitDuration = null;
        result.EffectiveWorkDuration = null;
        result.PresenceDuration = null;
        result.OvertimeDuration = null;
    }

    private static ExceptionResolution ResolveException(AttendanceCalculationContext context)
    {
        if (context.Exceptions.Count == 0 || context.Schedule is null || !context.Schedule.HasSchedule)
        {
            var single = context.Exceptions
                .OrderBy(x => x.Classify)
                .ThenBy(x => x.StartDateTime)
                .FirstOrDefault();

            return new ExceptionResolution(single, CalculateJustifiedDuration(single), CalculateJustifiedDayFraction(single), false);
        }

        var ordered = context.Exceptions
            .OrderBy(x => x.Classify == 0 ? 0 : 1)
            .ThenBy(x => x.StartDateTime)
            .ToList();

        var selected = ordered.FirstOrDefault();
        var bounds = ResolveScheduleBounds(context.CalculationDate, context.Schedule);
        var totalJustified = CalculateUniqueExceptionMinutes(ordered, bounds.ScheduledStart, bounds.ScheduledEnd);

        if (totalJustified > bounds.ScheduledEnd - bounds.ScheduledStart)
        {
            totalJustified = bounds.ScheduledEnd - bounds.ScheduledStart;
        }

        var dayFraction = (decimal)(totalJustified.TotalMinutes / Math.Max(1, (bounds.ScheduledEnd - bounds.ScheduledStart).TotalMinutes));
        var coversWholeDay = totalJustified >= bounds.ScheduledEnd - bounds.ScheduledStart && totalJustified > TimeSpan.Zero;
        return new ExceptionResolution(selected, totalJustified > TimeSpan.Zero ? totalJustified : null, dayFraction > 0 ? dayFraction : null, coversWholeDay);
    }

    private static TimeSpan? CalculateJustifiedDuration(AttendanceException? exception)
    {
        if (exception is null)
        {
            return null;
        }

        return exception.Unit switch
        {
            1 => TimeSpan.FromHours(exception.MinUnit),
            2 => TimeSpan.FromMinutes(exception.MinUnit),
            _ => exception.EndDateTime.HasValue ? exception.EndDateTime.Value - exception.StartDateTime : null
        };
    }

    private static decimal? CalculateJustifiedDayFraction(AttendanceException? exception)
    {
        if (exception is null)
        {
            return null;
        }

        return exception.Unit == 3 ? (decimal)exception.MinUnit : null;
    }

    private static ScheduleWindow ResolveEntryWindow(ScheduleBounds bounds, AttendanceSchedule schedule)
    {
        // [ASISTWEB][SEC.03.04]
        if (HasValidTimeRange(schedule.CheckInTime1, schedule.CheckInTime2))
        {
            return new ScheduleWindow(
                bounds.ScheduledStart - schedule.CheckInTime1!.Value.ToTimeSpan(),
                bounds.ScheduledStart + schedule.CheckInTime2!.Value.ToTimeSpan());
        }

        var halfSchedule = TimeSpan.FromMinutes((bounds.ScheduledEnd - bounds.ScheduledStart).TotalMinutes / 2d);
        return new ScheduleWindow(bounds.ScheduledStart - DefaultEntryWindowLead, bounds.ScheduledStart + halfSchedule);
    }

    private static ScheduleWindow ResolveExitWindow(ScheduleBounds bounds, AttendanceSchedule schedule)
    {
        // [ASISTWEB][SEC.03.04]
        if (HasValidTimeRange(schedule.CheckOutTime1, schedule.CheckOutTime2))
        {
            return new ScheduleWindow(
                bounds.ScheduledEnd - schedule.CheckOutTime1!.Value.ToTimeSpan(),
                bounds.ScheduledEnd + schedule.CheckOutTime2!.Value.ToTimeSpan());
        }

        var halfSchedule = TimeSpan.FromMinutes((bounds.ScheduledEnd - bounds.ScheduledStart).TotalMinutes / 2d);
        return new ScheduleWindow(bounds.ScheduledEnd - halfSchedule, bounds.ScheduledEnd + DefaultExitWindowTrail);
    }

    private static ScheduleBounds ResolveScheduleBounds(DateOnly date, AttendanceSchedule schedule)
    {
        var startDate = date.AddDays((schedule.StartDayOffset ?? 1) - 1);
        var endDate = date.AddDays((schedule.EndDayOffset ?? schedule.StartDayOffset ?? 1) - 1);
        var scheduledStart = CombineDateWithTime(startDate.ToDateTime(TimeOnly.MinValue).Date, schedule.ScheduledStartTime ?? TimeOnly.MinValue);
        var scheduledEnd = CombineDateWithTime(endDate.ToDateTime(TimeOnly.MinValue).Date, schedule.ScheduledEndTime ?? TimeOnly.MinValue);

        if (scheduledEnd <= scheduledStart)
        {
            scheduledEnd = scheduledEnd.AddDays(1);
        }

        return new ScheduleBounds(scheduledStart, scheduledEnd);
    }

    private static bool HasValidTimeRange(TimeOnly? start, TimeOnly? end)
        => start.HasValue && end.HasValue && start.Value < end.Value;

    private static bool IsWithinEntryWindow(DateTime timestamp, DateTime start, DateTime end)
        => timestamp >= start && timestamp < end;

    private static bool IsWithinExitWindow(DateTime timestamp, DateTime start, DateTime end)
        => timestamp >= start && timestamp <= end;

    private static DateTime CombineDateWithTime(DateTime date, TimeOnly time)
        => date.Date.Add(time.ToTimeSpan());

    private static TimeSpan ApplyAutomaticBreakDeduction(AttendanceSchedule? schedule, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var breakMinutes = schedule?.BreakMinutes ?? 0;
        if (breakMinutes <= 0)
        {
            return duration;
        }

        var breakDuration = TimeSpan.FromMinutes(breakMinutes);
        return duration > breakDuration ? duration - breakDuration : TimeSpan.Zero;
    }

    private static TimeSpan GetOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
    {
        var start = startA > startB ? startA : startB;
        var end = endA < endB ? endA : endB;
        return end > start ? end - start : TimeSpan.Zero;
    }

    private readonly record struct ScheduleBounds(DateTime ScheduledStart, DateTime ScheduledEnd);
    private readonly record struct ScheduleWindow(DateTime Start, DateTime End);
    private readonly record struct ExceptionResolution(AttendanceException? SelectedException, TimeSpan? JustifiedDuration, decimal? JustifiedDayFraction, bool CoversWholeScheduledDay);

    // [ASISTWEB][SEC.02][SEC.03][SEC.04]
    private static void AccumulateFinalDurations(AttendanceDayResult result, AttendancePersonAccumulation accumulation)
    {
        if (result.EffectiveWorkDuration.HasValue && result.EffectiveWorkDuration.Value > TimeSpan.Zero)
        {
            accumulation.HorasEfectivas += result.EffectiveWorkDuration.Value;
        }

        if (result.PresenceDuration.HasValue && result.PresenceDuration.Value > TimeSpan.Zero)
        {
            accumulation.HorasDePermanencia += result.PresenceDuration.Value;
        }

        if (result.LateEntryDuration.HasValue && result.LateEntryDuration.Value > TimeSpan.Zero)
        {
            accumulation.TardanzasDelDia += result.LateEntryDuration.Value;
        }

        if (result.EarlyExitDuration.HasValue && result.EarlyExitDuration.Value > TimeSpan.Zero)
        {
            accumulation.SalidasTempranoDelDia += result.EarlyExitDuration.Value;
        }

        if (result.OvertimeDuration.HasValue && result.OvertimeDuration.Value > TimeSpan.Zero)
        {
            accumulation.HorasExtras += result.OvertimeDuration.Value;
        }
    }

    private static void ApplyNoScheduleLabels(AttendanceCalculationContext context, AttendanceDayResult result)
    {
        if (context.IsHoliday)
        {
            result.ScheduleDisplayText = "FERIADO __:__ - __:__";
            return;
        }

        if (context.IsWeekend)
        {
            result.ScheduleDisplayText = "FIN DE SEMANA __:__ - __:__";
            return;
        }

        result.ScheduleDisplayText = "SIN TURNO __:__ - __:__";
    }

    private static void ApplySingleMarkExceptionAccumulation(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation)
    {
        if (context.Exceptions.Any(x => x.Unit == 3))
        {
            accumulation.DiasJustificados++;
        }
    }

    private static void ApplyNoScheduleExceptionAccumulation(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendancePersonAccumulation accumulation)
    {
        if (context.Exceptions.Any(x => x.Unit == 3))
        {
            accumulation.DiasJustificados++;
            return;
        }

        accumulation.HorasJustificadas += CalculateUniqueExceptionMinutes(context.Exceptions);
    }

    private static void ApplyAbsenceWithSchedule(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation, ScheduleBounds scheduleBounds, string observationCode)
    {
        result.IsAbsent = true;
        result.ScheduleObservationCode = observationCode;
        result.ScheduleDisplayText = BuildAssignedScheduleDisplayText(context, scheduleBounds, observationCode);
        accumulation.DiasConFalta++;
        ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);

        if (context.HasExceptions)
        {
            accumulation.DiasConFalta = Math.Max(0, accumulation.DiasConFalta - 1);
            result.IsAbsent = false;
            result.EffectiveWorkDuration = TimeSpan.Zero;
            result.PresenceDuration = TimeSpan.Zero;
            result.EntryMark = null;
            result.ExitMark = null;

            if (context.IsHoliday && context.Parameters.ShowHoliday)
            {
                accumulation.FeriadosConTurno++;
            }

            if (context.Exceptions.Any(x => x.Unit == 3))
            {
                accumulation.DiasJustificados++;
            }
            else
            {
                var programmed = scheduleBounds.ScheduledEnd - scheduleBounds.ScheduledStart - TimeSpan.FromMinutes(context.Schedule?.BreakMinutes ?? 0);
                if (programmed > TimeSpan.Zero)
                {
                    accumulation.HorasJustificadas += programmed;
                }
            }
        }
    }

    private static void ApplyScheduledExceptionRules(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation, ScheduleBounds scheduleBounds, ScheduledScenarioState state)
    {
        if (!context.HasExceptions)
        {
            return;
        }

        result.ExceptionDisplayText = BuildExceptionDisplayText(context, false);

        if (context.Exceptions.Any(x => x.Unit == 3))
        {
            accumulation.DiasJustificados++;
            result.LateEntryDuration = null;
            result.EarlyExitDuration = null;
            result.EffectiveWorkDuration = RecalculateEffectiveDuration(state.CaseKind, scheduleBounds, state.ActualEntryMark, state.ActualExitMark, TimeSpan.Zero, TimeSpan.Zero, context.Schedule?.BreakMinutes ?? 0);
            result.JustifiedDuration = null;
            return;
        }

        var justifiedLate = CalculateScheduledLateExceptionMinutes(context, result, scheduleBounds, state);
        var justifiedEarly = CalculateScheduledEarlyExceptionMinutes(context, result, scheduleBounds, state);
        var justifiedUnique = SumUniqueRanges(BuildUniqueJustificationRanges(context, scheduleBounds, state));

        var adjustedLate = (result.LateEntryDuration ?? TimeSpan.Zero) - justifiedLate;
        var adjustedEarly = (result.EarlyExitDuration ?? TimeSpan.Zero) - justifiedEarly;

        result.LateEntryDuration = adjustedLate > TimeSpan.Zero ? adjustedLate : null;
        result.EarlyExitDuration = adjustedEarly > TimeSpan.Zero ? adjustedEarly : null;

        if (justifiedUnique > TimeSpan.Zero)
        {
            accumulation.HorasJustificadas += justifiedUnique;
            result.JustifiedDuration = justifiedUnique;
        }

        result.EffectiveWorkDuration = RecalculateEffectiveDuration(
            state.CaseKind,
            scheduleBounds,
            state.ActualEntryMark,
            state.ActualExitMark,
            result.LateEntryDuration ?? TimeSpan.Zero,
            result.EarlyExitDuration ?? TimeSpan.Zero,
            context.Schedule?.BreakMinutes ?? 0);
    }

    private static List<(DateTime Start, DateTime End)> BuildUniqueJustificationRanges(AttendanceCalculationContext context, ScheduleBounds scheduleBounds, ScheduledScenarioState state)
    {
        var ranges = new List<(DateTime Start, DateTime End)>();

        if (state.ActualEntryMark > scheduleBounds.ScheduledStart)
        {
            ranges.AddRange(GetOverlappingRanges(context.Exceptions, scheduleBounds.ScheduledStart, state.ActualEntryMark.AddMinutes(1)));
        }

        if (state.ActualExitMark < scheduleBounds.ScheduledEnd)
        {
            ranges.AddRange(GetOverlappingRanges(context.Exceptions, state.ActualExitMark, scheduleBounds.ScheduledEnd.AddMinutes(1)));
        }

        return ranges;
    }

    private static IEnumerable<(DateTime Start, DateTime End)> GetOverlappingRanges(IReadOnlyList<AttendanceException> exceptions, DateTime rangeStart, DateTime rangeEnd)
    {
        return exceptions
            .Where(x => x.Unit is 1 or 2 && x.EndDateTime.HasValue)
            .Select(x => (
                Start: x.StartDateTime < rangeStart ? rangeStart : x.StartDateTime,
                End: x.EndDateTime!.Value > rangeEnd ? rangeEnd : x.EndDateTime!.Value))
            .Where(x => x.End > x.Start);
    }

    private static TimeSpan CalculateScheduledLateExceptionMinutes(AttendanceCalculationContext context, AttendanceDayResult result, ScheduleBounds scheduleBounds, ScheduledScenarioState state)
    {
        var lateDuration = result.LateEntryDuration ?? TimeSpan.Zero;
        if (lateDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (state.ActualEntryMark <= scheduleBounds.ScheduledStart)
        {
            return TimeSpan.Zero;
        }

        var justified = CalculateUniqueExceptionMinutes(context.Exceptions, scheduleBounds.ScheduledStart, state.ActualEntryMark.AddMinutes(1));
        return Min(justified, lateDuration);
    }

    private static TimeSpan CalculateScheduledEarlyExceptionMinutes(AttendanceCalculationContext context, AttendanceDayResult result, ScheduleBounds scheduleBounds, ScheduledScenarioState state)
    {
        var earlyDuration = result.EarlyExitDuration ?? TimeSpan.Zero;
        if (earlyDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (state.ActualExitMark >= scheduleBounds.ScheduledEnd)
        {
            return TimeSpan.Zero;
        }

        var justified = CalculateUniqueExceptionMinutes(context.Exceptions, state.ActualExitMark, scheduleBounds.ScheduledEnd.AddMinutes(1));
        return Min(justified, earlyDuration);
    }

    private static ScheduledScenarioState ProcessMissingEntryScenario(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation, ScheduleBounds scheduleBounds, AttendanceMark exitMark)
    {
        result.InconsistencyKind = AttendanceInconsistencyKind.MissingEntry;
        result.EntryMark = null;

        if (context.Parameters.NoInAbsent == 2)
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(e)");
            return ScheduledScenarioState.Terminal();
        }

        var assumedEntry = scheduleBounds.ScheduledStart;
        var late = context.Parameters.NoInAbsent == 1 ? TimeSpan.FromMinutes(Math.Max(0, context.Parameters.MinsNoIn)) : TimeSpan.Zero;
        result.LateEntryDuration = late > TimeSpan.Zero ? late : null;
        result.EarlyExitDuration = CalculateEarlyExitLiteral(scheduleBounds.ScheduledEnd, exitMark.Timestamp, context.Schedule?.EarlyToleranceMinutes ?? 0);

        if (TriggersEarlyAbsence(context, result.EarlyExitDuration))
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(s*)");
            return ScheduledScenarioState.Terminal();
        }

        result.PresenceDuration = CalculatePresenceDuration(assumedEntry, exitMark.Timestamp, context.Schedule?.BreakMinutes ?? 0);
        result.EffectiveWorkDuration = RecalculateEffectiveDuration(ScheduledCaseKind.MissingEntry, scheduleBounds, assumedEntry, exitMark.Timestamp, late, result.EarlyExitDuration ?? TimeSpan.Zero, context.Schedule?.BreakMinutes ?? 0);
        return ScheduledScenarioState.Continue(ScheduledCaseKind.MissingEntry, assumedEntry, exitMark.Timestamp, assumedEntry, exitMark.Timestamp);
    }

    private static ScheduledScenarioState ProcessMissingExitScenario(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation, ScheduleBounds scheduleBounds, AttendanceMark entryMark)
    {
        result.InconsistencyKind = AttendanceInconsistencyKind.MissingExit;
        result.ExitMark = null;

        if (context.Parameters.NoOutAbsent == 2)
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(s)");
            return ScheduledScenarioState.Terminal();
        }

        var assumedExit = scheduleBounds.ScheduledEnd;
        var early = context.Parameters.NoOutAbsent == 1 ? TimeSpan.FromMinutes(Math.Max(0, context.Parameters.MinsNoLeave)) : TimeSpan.Zero;
        result.EarlyExitDuration = early > TimeSpan.Zero ? early : null;
        result.LateEntryDuration = CalculateLateLiteral(scheduleBounds.ScheduledStart, entryMark.Timestamp, context.Schedule?.LateToleranceMinutes ?? 0);

        if (TriggersLateAbsence(context, result.LateEntryDuration))
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(e*)");
            return ScheduledScenarioState.Terminal();
        }

        result.PresenceDuration = CalculatePresenceDuration(entryMark.Timestamp, assumedExit, context.Schedule?.BreakMinutes ?? 0);
        result.EffectiveWorkDuration = RecalculateEffectiveDuration(ScheduledCaseKind.MissingExit, scheduleBounds, entryMark.Timestamp, assumedExit, result.LateEntryDuration ?? TimeSpan.Zero, early, context.Schedule?.BreakMinutes ?? 0);
        return ScheduledScenarioState.Continue(ScheduledCaseKind.MissingExit, entryMark.Timestamp, assumedExit, entryMark.Timestamp, assumedExit);
    }

    private static ScheduledScenarioState ProcessEntryAndExitScenario(AttendanceCalculationContext context, ExceptionResolution resolvedException, AttendanceDayResult result, AttendancePersonAccumulation accumulation, ScheduleBounds scheduleBounds, AttendanceMark entryMark, AttendanceMark exitMark)
    {
        var late = CalculateLateLiteral(scheduleBounds.ScheduledStart, entryMark.Timestamp, context.Schedule?.LateToleranceMinutes ?? 0);
        result.LateEntryDuration = late > TimeSpan.Zero ? late : null;

        if (TriggersLateAbsence(context, result.LateEntryDuration))
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(e*)");
            return ScheduledScenarioState.Terminal();
        }

        var early = CalculateEarlyExitLiteral(scheduleBounds.ScheduledEnd, exitMark.Timestamp, context.Schedule?.EarlyToleranceMinutes ?? 0);
        result.EarlyExitDuration = early > TimeSpan.Zero ? early : null;

        if (TriggersEarlyAbsence(context, result.EarlyExitDuration))
        {
            ApplyAbsenceWithSchedule(context, resolvedException, result, accumulation, scheduleBounds, "(s*)");
            return ScheduledScenarioState.Terminal();
        }

        result.PresenceDuration = CalculatePresenceDuration(entryMark.Timestamp, exitMark.Timestamp, context.Schedule?.BreakMinutes ?? 0);
        result.EffectiveWorkDuration = RecalculateEffectiveDuration(ScheduledCaseKind.EntryAndExit, scheduleBounds, entryMark.Timestamp, exitMark.Timestamp, late, early, context.Schedule?.BreakMinutes ?? 0);
        return ScheduledScenarioState.Continue(ScheduledCaseKind.EntryAndExit, entryMark.Timestamp, exitMark.Timestamp, entryMark.Timestamp, exitMark.Timestamp);
    }

    private static TimeSpan RecalculateEffectiveDuration(ScheduledCaseKind caseKind, ScheduleBounds scheduleBounds, DateTime actualEntry, DateTime actualExit, TimeSpan late, TimeSpan early, int breakMinutes)
    {
        var breakDuration = TimeSpan.FromMinutes(Math.Max(0, breakMinutes));
        TimeSpan value = caseKind switch
        {
            ScheduledCaseKind.MissingEntry => actualExit >= scheduleBounds.ScheduledEnd
                ? scheduleBounds.ScheduledEnd - scheduleBounds.ScheduledStart - late - early - breakDuration
                : actualExit - scheduleBounds.ScheduledStart - late - breakDuration,
            ScheduledCaseKind.MissingExit => actualEntry <= scheduleBounds.ScheduledStart
                ? scheduleBounds.ScheduledEnd - scheduleBounds.ScheduledStart - late - early - breakDuration
                : scheduleBounds.ScheduledEnd - actualEntry - early - breakDuration,
            _ => scheduleBounds.ScheduledEnd - scheduleBounds.ScheduledStart - late - early - breakDuration
        };

        return value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    private static TimeSpan CalculateLateLiteral(DateTime scheduledStart, DateTime actualEntry, int toleranceMinutes)
    {
        var diff = actualEntry <= scheduledStart ? TimeSpan.Zero : actualEntry - scheduledStart;
        return diff.TotalMinutes > Math.Max(0, toleranceMinutes)
            ? diff
            : TimeSpan.Zero;
    }

    private static TimeSpan CalculateEarlyExitLiteral(DateTime scheduledEnd, DateTime actualExit, int toleranceMinutes)
    {
        var diff = actualExit >= scheduledEnd ? TimeSpan.Zero : scheduledEnd - actualExit;
        return diff.TotalMinutes > Math.Max(0, toleranceMinutes)
            ? diff
            : TimeSpan.Zero;
    }

    private static bool TriggersLateAbsence(AttendanceCalculationContext context, TimeSpan? late)
        => context.Parameters.LateAbsent && context.Parameters.MinsLateAbsent > 0 && late.HasValue && late.Value.TotalMinutes > context.Parameters.MinsLateAbsent;

    private static bool TriggersEarlyAbsence(AttendanceCalculationContext context, TimeSpan? early)
        => context.Parameters.EarlyAbsent && context.Parameters.MinsEarlyAbsent > 0 && early.HasValue && early.Value.TotalMinutes > context.Parameters.MinsEarlyAbsent;

    private static TimeSpan SubtractBreak(TimeSpan duration, int breakMinutes)
    {
        if (duration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var deduction = TimeSpan.FromMinutes(Math.Max(0, breakMinutes));
        var value = duration - deduction;
        return value > TimeSpan.Zero ? value : TimeSpan.Zero;
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right)
        => left <= right ? left : right;

    private static TimeSpan CalculatePresenceDuration(DateTime actualEntry, DateTime actualExit, int breakMinutes)
        => SubtractBreak(actualExit - actualEntry, breakMinutes);

    private readonly record struct ScheduledScenarioState(
        ScheduledCaseKind CaseKind,
        DateTime EntryForCalculations,
        DateTime ExitForCalculations,
        DateTime ActualEntryMark,
        DateTime ActualExitMark,
        bool IsTerminal)
    {
        public static ScheduledScenarioState Continue(ScheduledCaseKind caseKind, DateTime entryForCalculations, DateTime exitForCalculations, DateTime actualEntryMark, DateTime actualExitMark)
            => new(caseKind, entryForCalculations, exitForCalculations, actualEntryMark, actualExitMark, false);

        public static ScheduledScenarioState Terminal()
            => new(ScheduledCaseKind.None, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, true);
    }

    private enum ScheduledCaseKind
    {
        None = 0,
        MissingEntry = 1,
        MissingExit = 2,
        EntryAndExit = 3
    }

    private static TimeSpan CalculateOverlapForRange(IReadOnlyList<AttendanceException> exceptions, DateTime rangeStart, DateTime rangeEnd)
    {
        return CalculateUniqueExceptionMinutes(exceptions, rangeStart, rangeEnd);
    }

    private static TimeSpan CalculateUniqueExceptionMinutes(IReadOnlyList<AttendanceException> exceptions)
    {
        if (exceptions.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var ranges = exceptions
            .Where(x => x.Unit is 1 or 2 && x.EndDateTime.HasValue)
            .Select(x => (Start: x.StartDateTime, End: x.EndDateTime!.Value))
            .OrderBy(x => x.Start)
            .ToList();

        return SumUniqueRanges(ranges);
    }

    private static TimeSpan CalculateUniqueExceptionMinutes(IReadOnlyList<AttendanceException> exceptions, DateTime rangeStart, DateTime rangeEnd)
    {
        var ranges = exceptions
            .Where(x => x.Unit is 1 or 2 && x.EndDateTime.HasValue)
            .Select(x => (Start: x.StartDateTime < rangeStart ? rangeStart : x.StartDateTime, End: x.EndDateTime!.Value > rangeEnd ? rangeEnd : x.EndDateTime!.Value))
            .Where(x => x.End > x.Start)
            .OrderBy(x => x.Start)
            .ToList();

        return SumUniqueRanges(ranges);
    }

    private static TimeSpan SumUniqueRanges(IReadOnlyList<(DateTime Start, DateTime End)> ranges)
    {
        if (ranges.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var total = TimeSpan.Zero;
        var currentStart = ranges[0].Start;
        var currentEnd = ranges[0].End;

        for (var i = 1; i < ranges.Count; i++)
        {
            var range = ranges[i];
            if (range.Start <= currentEnd)
            {
                if (range.End > currentEnd)
                {
                    currentEnd = range.End;
                }

                continue;
            }

            total += currentEnd - currentStart;
            currentStart = range.Start;
            currentEnd = range.End;
        }

        total += currentEnd - currentStart;
        return total;
    }

    private static string BuildExceptionDisplayText(AttendanceCalculationContext context, bool isNoSchedule)
    {
        if (!context.HasExceptions)
        {
            return string.Empty;
        }

        var ordered = isNoSchedule
            ? context.Exceptions.OrderBy(x => x.Classify == 128 ? 0 : 1).ThenBy(x => x.Unit).ThenBy(x => x.Classify).ThenBy(x => x.LeaveId)
            : context.Exceptions.OrderBy(x => x.Classify == 0 ? 0 : 1).ThenBy(x => x.Unit).ThenBy(x => x.Classify).ThenBy(x => x.LeaveId);

        return string.Join(", ", ordered.Select(x => x.LeaveName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildInitialScheduleDisplayText(AttendanceCalculationContext context)
    {
        if (context.Schedule is null || !context.Schedule.HasSchedule)
        {
            return string.Empty;
        }

        var startText = context.Schedule.ScheduledStartTime?.ToString("HH:mm") ?? "__:__";
        var endText = context.Schedule.ScheduledEndTime?.ToString("HH:mm") ?? "__:__";
        return $"{context.Schedule.ScheduleName} {startText} - {endText}";
    }

    private static string BuildAssignedScheduleDisplayText(AttendanceCalculationContext context, ScheduleBounds bounds, string observationCode)
    {
        var suffix = context.IsHoliday && context.Parameters.ShowHoliday
            ? "(FER)"
            : !context.IsHoliday && context.IsWeekend
                ? "(FDS)"
                : string.Empty;

        return $"{context.Schedule?.ScheduleName} {suffix}{observationCode} {bounds.ScheduledStart:HH:mm} - {bounds.ScheduledEnd:HH:mm}".Trim();
    }
}