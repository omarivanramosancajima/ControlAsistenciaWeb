namespace ControlAsistencia.Web.Models;

public sealed class HomeRealtimeAttendanceViewModel
{
    public IReadOnlyList<HomeRealtimeAttendanceItemViewModel> Items { get; init; }
        = Array.Empty<HomeRealtimeAttendanceItemViewModel>();
}

public sealed class HomeRealtimeAttendanceItemViewModel
{
    public int UserId { get; init; }
    public string BadgeNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CheckTime { get; init; }
    public string Area { get; init; } = string.Empty;
    public string? PhotoBase64 { get; init; }
}
