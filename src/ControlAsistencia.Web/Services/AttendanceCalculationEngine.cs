using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Services;

public class AttendanceCalculationEngine : IAttendanceCalculationEngine
{
    private static readonly TimeSpan DefaultEntryWindowLead = TimeSpan.FromHours(2);
    private static readonly TimeSpan DefaultExitWindowTrail = TimeSpan.FromHours(6);
    private static readonly TimeSpan HrBreakDeduction = TimeSpan.FromMinutes(6);
    private static readonly TimeSpan HnBreakDeduction = TimeSpan.FromMinutes(90);

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
        result.Exception = resolvedException.SelectedException;
        result.JustifiedDuration = resolvedException.JustifiedDuration;
        result.JustifiedDayFraction = resolvedException.JustifiedDayFraction;

        if (context.IsNoSchedule || context.Schedule is null || !context.Schedule.HasSchedule)
        {
            CalculateNoSchedule(context, orderedMarks, orderedNextDayMarks, resolvedException, result);
            return result;
        }

        CalculateScheduledDay(context, orderedMarks, resolvedException, result);
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
        AttendanceDayResult result)
    {
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
                result.IntermediateMarks = Array.Empty<AttendanceMark>();
                result.InconsistencyKind = AttendanceInconsistencyKind.SingleMarkWithoutNextDayClosure;
                result.IsAbsent = false;
                return;
            }

            result.ExitMark = nextDayClosure;
            result.IntermediateMarks = Array.Empty<AttendanceMark>();

            var rawDuration = nextDayClosure.Timestamp - orderedMarks[0].Timestamp;
            var netDuration = ApplyAutomaticBreakDeduction(context.Schedule?.ScheduleName, rawDuration);

            result.EffectiveWorkDuration = netDuration;
            result.PresenceDuration = netDuration;
            result.OvertimeDuration = ResolveNoScheduleOvertime(context, netDuration);
            result.IsAbsent = false;
            return;
        }

        result.EntryMark = orderedMarks.First();
        result.ExitMark = orderedMarks.Last();
        result.IntermediateMarks = orderedMarks.Skip(1).Take(orderedMarks.Count - 2).ToList();

        var duration = result.ExitMark.Timestamp - result.EntryMark.Timestamp;
        var effective = ApplyAutomaticBreakDeduction(context.Schedule?.ScheduleName, duration);
        result.EffectiveWorkDuration = effective;
        result.PresenceDuration = effective;
        result.OvertimeDuration = ResolveNoScheduleOvertime(context, effective);
        result.IsAbsent = false;
    }

    private static void CalculateScheduledDay(
        AttendanceCalculationContext context,
        IReadOnlyList<AttendanceMark> orderedMarks,
        ExceptionResolution resolvedException,
        AttendanceDayResult result)
    {
        var schedule = context.Schedule!;
        var scheduleBounds = ResolveScheduleBounds(context.CalculationDate, schedule);
        var entryWindow = ResolveEntryWindow(scheduleBounds, schedule);
        var exitWindow = ResolveExitWindow(scheduleBounds, schedule);

        var entryMark = orderedMarks.FirstOrDefault(mark => IsWithinWindow(mark.Timestamp, entryWindow.Start, entryWindow.End));
        var exitMark = orderedMarks.LastOrDefault(mark => !mark.IsPreviousDayClosureMark && IsWithinWindow(mark.Timestamp, exitWindow.Start, exitWindow.End));

        result.EntryMark = entryMark;
        result.ExitMark = exitMark;
        result.IntermediateMarks = orderedMarks
            .Where(mark => !ReferenceEquals(mark, entryMark) && !ReferenceEquals(mark, exitMark))
            .ToList();

        var entryTimeForCalculations = entryMark?.Timestamp;
        var exitTimeForCalculations = exitMark?.Timestamp;
        var missingEntry = entryMark is null;
        var missingExit = exitMark is null;

        if (missingEntry)
        {
            switch (context.Parameters.NoInAbsent)
            {
                case 0 when exitMark is not null:
                    entryTimeForCalculations = scheduleBounds.ScheduledStart;
                    break;
                case 1:
                    entryTimeForCalculations = scheduleBounds.ScheduledStart.AddMinutes(context.Parameters.MinsNoIn);
                    result.LateEntryDuration = TimeSpan.FromMinutes(context.Parameters.MinsNoIn);
                    break;
                case 2:
                    result.IsAbsent = true;
                    result.InconsistencyKind = AttendanceInconsistencyKind.MissingEntry;
                    ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);
                    return;
            }
        }

        if (missingExit)
        {
            switch (context.Parameters.NoOutAbsent)
            {
                case 0 when entryTimeForCalculations is not null:
                    exitTimeForCalculations = scheduleBounds.ScheduledEnd;
                    break;
                case 1:
                    exitTimeForCalculations = scheduleBounds.ScheduledEnd.AddMinutes(-context.Parameters.MinsNoLeave);
                    result.EarlyExitDuration = TimeSpan.FromMinutes(context.Parameters.MinsNoLeave);
                    break;
                case 2:
                    result.IsAbsent = true;
                    if (result.InconsistencyKind == AttendanceInconsistencyKind.None)
                    {
                        result.InconsistencyKind = AttendanceInconsistencyKind.MissingExit;
                    }
                    ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);
                    return;
            }
        }

        if (entryTimeForCalculations is null && exitTimeForCalculations is null)
        {
            result.IsAbsent = true;
            ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);
            return;
        }

        if (entryTimeForCalculations is null || exitTimeForCalculations is null)
        {
            result.IsAbsent = true;
            if (entryTimeForCalculations is null)
            {
                result.InconsistencyKind = AttendanceInconsistencyKind.MissingEntry;
            }
            else if (exitTimeForCalculations is null)
            {
                result.InconsistencyKind = AttendanceInconsistencyKind.MissingExit;
            }

            ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);
            return;
        }

        var scheduledDuration = scheduleBounds.ScheduledEnd - scheduleBounds.ScheduledStart;
        var actualDuration = exitTimeForCalculations.Value - entryTimeForCalculations.Value;
        var effectiveDuration = ApplyAutomaticBreakDeduction(schedule.ScheduleName, actualDuration);
        var presenceDuration = effectiveDuration;

        var computedLate = result.LateEntryDuration ?? CalculateLateDuration(scheduleBounds.ScheduledStart, entryTimeForCalculations.Value, schedule.LateToleranceMinutes ?? 0);
        var computedEarly = result.EarlyExitDuration ?? CalculateEarlyExitDuration(scheduleBounds.ScheduledEnd, exitTimeForCalculations.Value, schedule.EarlyToleranceMinutes ?? 0);

        computedLate = AdjustDurationByException(scheduleBounds.ScheduledStart, entryTimeForCalculations.Value, computedLate, resolvedException.SelectedException);
        computedEarly = AdjustDurationByException(exitTimeForCalculations.Value, scheduleBounds.ScheduledEnd, computedEarly, resolvedException.SelectedException);

        result.LateEntryDuration = computedLate > TimeSpan.Zero ? computedLate : null;
        result.EarlyExitDuration = computedEarly > TimeSpan.Zero ? computedEarly : null;

        if (context.Parameters.LateAbsent && result.LateEntryDuration.HasValue && result.LateEntryDuration.Value.TotalMinutes > context.Parameters.MinsLateAbsent)
        {
            result.IsAbsent = true;
        }

        if (context.Parameters.EarlyAbsent && result.EarlyExitDuration.HasValue && result.EarlyExitDuration.Value.TotalMinutes > context.Parameters.MinsEarlyAbsent)
        {
            result.IsAbsent = true;
        }

        var hadFullDayException = resolvedException.CoversWholeScheduledDay;
        ApplyFullDayExceptionIfAny(scheduleBounds, resolvedException, result);

        if (!result.IsAbsent || hadFullDayException)
        {
            result.EffectiveWorkDuration = effectiveDuration;
            result.PresenceDuration = presenceDuration;
            result.OvertimeDuration = ResolveScheduledOvertime(context, scheduleBounds, entryTimeForCalculations.Value, exitTimeForCalculations.Value, scheduledDuration);

            if (context.IsWeekend && context.Parameters.WeekenFullDayOT)
            {
                result.OvertimeDuration = effectiveDuration;
            }
        }

        if (missingEntry && result.InconsistencyKind == AttendanceInconsistencyKind.None)
        {
            result.InconsistencyKind = AttendanceInconsistencyKind.MissingEntry;
        }

        if (missingExit && result.InconsistencyKind == AttendanceInconsistencyKind.None)
        {
            result.InconsistencyKind = AttendanceInconsistencyKind.MissingExit;
        }
    }

    private static TimeSpan ResolveScheduledOvertime(
        AttendanceCalculationContext context,
        ScheduleBounds scheduleBounds,
        DateTime actualEntry,
        DateTime actualExit,
        TimeSpan scheduledDuration)
    {
        if (context.IsHoliday && context.Parameters.ShowHoliday && context.Parameters.AllowHolidayOT)
        {
            var holidayOt = ApplyLimit(actualExit - actualEntry, context.Parameters.LimitHolidayOT);
            return ApplyAutomaticBreakDeduction(context.Schedule?.ScheduleName, holidayOt);
        }

        if (context.IsWeekend && context.Parameters. WeekenFullDayOT)
        {
            return ApplyAutomaticBreakDeduction(context.Schedule?.ScheduleName, actualExit - actualEntry);
        }

        var earlyOt = TimeSpan.Zero;
        if (context.Parameters.AllowEarlyOT)
        {
            var earlyDiff = scheduleBounds.ScheduledStart - actualEntry;
            if (earlyDiff.TotalMinutes >= context.Parameters.IntervalOfEarlyOT)
            {
                earlyOt = TimeSpan.FromMinutes(Math.Max(0, earlyDiff.TotalMinutes - context.Parameters.IntervalOfEarlyOTAlternate));
                if (context.Parameters.LimitEarlyMaxOT)
                {
                    earlyOt = ApplyLimit(earlyOt, context.Parameters.EarlyMaxOT);
                }
            }
        }

        var afterOt = TimeSpan.Zero;
        if (context.Parameters.AllowAfterOT)
        {
            var afterDiff = actualExit - scheduleBounds.ScheduledEnd;
            if (afterDiff.TotalMinutes >= context.Parameters.IntervalOfAfterOT)
            {
                afterOt = TimeSpan.FromMinutes(Math.Max(0, afterDiff.TotalMinutes - context.Parameters.IntervalOfAfterOTAlternate));
                if (context.Parameters.LimitAfterMaxOT)
                {
                    afterOt = ApplyLimit(afterOt, context.Parameters.AfterMaxOT);
                }
            }
        }

        var totalOt = earlyOt + afterOt;
        return totalOt > scheduledDuration ? scheduledDuration : totalOt;
    }

    private static TimeSpan ResolveNoScheduleOvertime(AttendanceCalculationContext context, TimeSpan effectiveDuration)
    {
        if (context.IsHoliday)
        {
            if (!context.Parameters.ShowHoliday || !context.Parameters.AllowHolidayOT)
            {
                return TimeSpan.Zero;
            }

            return ApplyLimit(effectiveDuration, context.Parameters.LimitHolidayOT);
        }

        if (context.IsWeekend && context.Parameters.WeekenFullDayOT)
        {
            return effectiveDuration;
        }

        if (!context.Parameters.ShowNoTurn || !context.Parameters.AllowNoTurnOT)
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
        var totalJustified = TimeSpan.Zero;
        foreach (var exception in ordered)
        {
            totalJustified += GetOverlap(bounds.ScheduledStart, bounds.ScheduledEnd, exception.StartDateTime, exception.EndDateTime ?? exception.StartDateTime);
        }

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

    private static TimeSpan CalculateLateDuration(DateTime scheduledStart, DateTime actualEntry, int toleranceMinutes)
    {
        var late = actualEntry - scheduledStart - TimeSpan.FromMinutes(Math.Max(0, toleranceMinutes));
        return late > TimeSpan.Zero ? late : TimeSpan.Zero;
    }

    private static TimeSpan CalculateEarlyExitDuration(DateTime scheduledEnd, DateTime actualExit, int toleranceMinutes)
    {
        var early = scheduledEnd - actualExit - TimeSpan.FromMinutes(Math.Max(0, toleranceMinutes));
        return early > TimeSpan.Zero ? early : TimeSpan.Zero;
    }

    private static ScheduleWindow ResolveEntryWindow(ScheduleBounds bounds, AttendanceSchedule schedule)
    {
        if (HasValidTimeRange(schedule.CheckInTime1, schedule.CheckInTime2))
        {
            return new ScheduleWindow(
                CombineDateWithTime(bounds.ScheduledStart.Date, schedule.CheckInTime1!.Value),
                CombineDateWithTime(bounds.ScheduledStart.Date, schedule.CheckInTime2!.Value));
        }

        var halfSchedule = TimeSpan.FromMinutes((bounds.ScheduledEnd - bounds.ScheduledStart).TotalMinutes / 2d);
        return new ScheduleWindow(bounds.ScheduledStart - DefaultEntryWindowLead, bounds.ScheduledStart + halfSchedule);
    }

    private static ScheduleWindow ResolveExitWindow(ScheduleBounds bounds, AttendanceSchedule schedule)
    {
        if (HasValidTimeRange(schedule.CheckOutTime1, schedule.CheckOutTime2))
        {
            var baseDate = bounds.ScheduledEnd.Date;
            return new ScheduleWindow(
                CombineDateWithTime(baseDate, schedule.CheckOutTime1!.Value),
                CombineDateWithTime(baseDate, schedule.CheckOutTime2!.Value));
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

    private static bool IsWithinWindow(DateTime timestamp, DateTime start, DateTime end)
        => timestamp >= start && timestamp <= end;

    private static DateTime CombineDateWithTime(DateTime date, TimeOnly time)
        => date.Date.Add(time.ToTimeSpan());

    private static TimeSpan ApplyAutomaticBreakDeduction(string? scheduleName, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (string.IsNullOrWhiteSpace(scheduleName))
        {
            return duration;
        }

        if (scheduleName.StartsWith("HR", StringComparison.OrdinalIgnoreCase))
        {
            return duration > HrBreakDeduction ? duration - HrBreakDeduction : TimeSpan.Zero;
        }

        if (scheduleName.StartsWith("HN", StringComparison.OrdinalIgnoreCase))
        {
            return duration > HnBreakDeduction ? duration - HnBreakDeduction : TimeSpan.Zero;
        }

        return duration;
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
}