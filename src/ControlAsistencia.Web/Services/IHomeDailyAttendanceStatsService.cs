namespace ControlAsistencia.Web.Services;

public interface IHomeDailyAttendanceStatsService
{
    Task<Models.HomeDailyAttendanceStatsViewModel> GetTodayAsync(
        CancellationToken cancellationToken = default);
}
