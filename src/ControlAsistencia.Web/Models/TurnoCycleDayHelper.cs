namespace ControlAsistencia.Web.Models;

public static class TurnoCycleDayHelper
{
    private static readonly string[] WeeklyDayNames = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];

    public static IReadOnlyList<TurnoDiaViewModel> BuildDays(int units, int cyle, IEnumerable<NumRunDeilAsignacionDTO>? assignments = null)
    {
        var totalDays = GetTotalDays(units, cyle);
        var assignmentMap = (assignments ?? Array.Empty<NumRunDeilAsignacionDTO>())
            .GroupBy(x => x.SDAYS)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<TurnoDiaViewModel>(totalDays);
        for (var day = 1; day <= totalDays; day++)
        {
            assignmentMap.TryGetValue(day, out var assignment);
            result.Add(new TurnoDiaViewModel
            {
                DayNumber = day,
                DayLabel = GetDayLabel(units, day),
                SchClassId = assignment?.SCHCLASSID,
                SchName = assignment?.SchName,
                StartTime = assignment?.STARTTIME,
                EndTime = assignment?.ENDTIME,
                Color = assignment?.Color
            });
        }

        return result;
    }

    public static int GetTotalDays(int units, int cyle) => units switch
    {
        0 => cyle,
        1 => 7,
        2 => 30,
        3 => 15,
        _ => 0
    };

    public static string GetFrequencyLabel(int units) => units switch
    {
        0 => "Diario",
        1 => "Semanal",
        2 => "Mensual",
        3 => "Quincenal",
        _ => string.Empty
    };

    public static string GetDayLabel(int units, int dayNumber)
    {
        if (units == 1 && dayNumber >= 1 && dayNumber <= WeeklyDayNames.Length)
        {
            return WeeklyDayNames[dayNumber - 1];
        }

        return $"Día {dayNumber:00}";
    }
}