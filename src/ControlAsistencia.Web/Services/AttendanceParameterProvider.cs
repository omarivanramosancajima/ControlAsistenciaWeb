using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace ControlAsistencia.Web.Services;

public class AttendanceParameterProvider : IAttendanceParameterProvider
{
    private readonly string _connectionString;

    public AttendanceParameterProvider(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<AttendanceCalculationParameters> GetParametersAsync()
    {
        // [ASISTWEB][SEC.01.02]
        // [ASISTWEB][SEC.02]
        // [ASISTWEB][SEC.03.06.03]
        // [ASISTWEB][SEC.03.06.04]
        const string sql = @"
SELECT
    PARANAME AS ParameterName,
    PARAVALUE AS ParameterValue
FROM dbo.AttParam WITH (NOLOCK)
WHERE PARANAME IN
(
    @AllowAfterOT,
    @AllowEarlyOT,
    @IntervalOfAfterOT,
    @IntervalOfEarlyOT,
    @LimitAfterMaxOT,
    @AfterMaxOT,
    @LimitEarlyMaxOT,
    @EarlyMaxOT,
    @NoInAbsent,
    @MinsNoIn,
    @NoOutAbsent,
    @MinsNoLeave,
    @EarlyAbsent,
    @MinsEarlyAbsent,
    @LateAbsent,
    @MinsLateAbsent,
    @ShowNoTurn,
    @AllowNoTurnOT,
    @LimitNoTurnOT,
    @ShowHoliday,
    @AllowHolidayOT,
    @LimitHolidayOT,
    @ShowWeekends,
    @WeekenFullDayOT,
    @Weekends
);";

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            var rows = (await connection.QueryAsync<AttendanceParameterRow>(sql, new
            {
                AllowAfterOT = "AllowAfterOT",
                AllowEarlyOT = "AllowEarlyOT",
                IntervalOfAfterOT = "IntervalOfAfterOT",
                IntervalOfEarlyOT = "IntervalOfEarlyOT",
                LimitAfterMaxOT = "LimitAfterMaxOT",
                AfterMaxOT = "AfterMaxOT",
                LimitEarlyMaxOT = "LimitEarlyMaxOT",
                EarlyMaxOT = "EarlyMaxOT",
                NoInAbsent = "NoInAbsent",
                MinsNoIn = "MinsNoIn",
                NoOutAbsent = "NoOutAbsent",
                MinsNoLeave = "MinsNoLeave",
                EarlyAbsent = "EarlyAbsent",
                MinsEarlyAbsent = "MinsEarlyAbsent",
                LateAbsent = "LateAbsent",
                MinsLateAbsent = "MinsLateAbsent",
                ShowNoTurn = "ShowNoTurn",
                AllowNoTurnOT = "AllowNoTurnOT",
                LimitNoTurnOT = "LimitNoTurnOT",
                ShowHoliday = "ShowHoliday",
                AllowHolidayOT = "AllowHolidayOT",
                LimitHolidayOT = "LimitHolidayOT",
                ShowWeekends = "Showweekends",
                WeekenFullDayOT = "WeekenFullDayOT",
                Weekends = "weekends"
            })).ToDictionary(x => x.ParameterName, x => x.ParameterValue, StringComparer.OrdinalIgnoreCase);

            return new AttendanceCalculationParameters
            {
                AllowAfterOT = GetBool(rows, "AllowAfterOT"),
                AllowEarlyOT = GetBool(rows, "AllowEarlyOT"),
                IntervalOfAfterOT = GetInt(rows, "IntervalOfAfterOT"),
                IntervalOfEarlyOT = GetInt(rows, "IntervalOfEarlyOT"),
                LimitAfterMaxOT = GetBool(rows, "LimitAfterMaxOT"),
                AfterMaxOT = GetInt(rows, "AfterMaxOT"),
                LimitEarlyMaxOT = GetBool(rows, "LimitEarlyMaxOT"),
                EarlyMaxOT = GetInt(rows, "EarlyMaxOT"),
                NoInAbsent = GetInt(rows, "NoInAbsent"),
                MinsNoIn = GetInt(rows, "MinsNoIn"),
                NoOutAbsent = GetInt(rows, "NoOutAbsent"),
                MinsNoLeave = GetInt(rows, "MinsNoLeave"),
                EarlyAbsent = GetBool(rows, "EarlyAbsent"),
                MinsEarlyAbsent = GetInt(rows, "MinsEarlyAbsent"),
                LateAbsent = GetBool(rows, "LateAbsent"),
                MinsLateAbsent = GetInt(rows, "MinsLateAbsent"),
                ShowNoTurn = GetBool(rows, "ShowNoTurn"),
                AllowNoTurnOT = GetBool(rows, "AllowNoTurnOT"),
                LimitNoTurnOT = GetInt(rows, "LimitNoTurnOT"),
                ShowHoliday = GetBool(rows, "ShowHoliday"),
                AllowHolidayOT = GetBool(rows, "AllowHolidayOT"),
                LimitHolidayOT = GetInt(rows, "LimitHolidayOT"),
                ShowWeekends = GetBool(rows, "Showweekends"),
                WeekenFullDayOT = GetBool(rows, "WeekenFullDayOT"),
                WeekendsRaw = GetString(rows, "weekends") ?? string.Empty
            };
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException("Ocurrió un error SQL al obtener los parámetros de asistencia.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ocurrió un error inesperado al obtener los parámetros de asistencia.", ex);
        }
    }

    private static bool GetBool(IReadOnlyDictionary<string, string?> values, string key)
        => int.TryParse(GetString(values, key), out var parsed) && parsed != 0;

    private static int GetInt(IReadOnlyDictionary<string, string?> values, string key)
        => int.TryParse(GetString(values, key), out var parsed) ? parsed : 0;

    private static string? GetString(IReadOnlyDictionary<string, string?> values, string key)
        => values.TryGetValue(key, out var value) ? value : null;

    private sealed class AttendanceParameterRow
    {
        public string ParameterName { get; set; } = string.Empty;
        public string? ParameterValue { get; set; }
    }
}