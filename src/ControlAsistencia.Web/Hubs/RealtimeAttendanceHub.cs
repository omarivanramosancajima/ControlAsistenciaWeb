using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ControlAsistencia.Web.Hubs;

[Authorize]
public sealed class RealtimeAttendanceHub : Hub
{
}
