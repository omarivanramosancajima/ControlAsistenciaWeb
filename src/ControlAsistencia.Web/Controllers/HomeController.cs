using System.Diagnostics;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly RealtimeAttendanceService _realtimeAttendanceService;

    public HomeController(
        ILogger<HomeController> logger,
        RealtimeAttendanceService realtimeAttendanceService)
    {
        _logger = logger;
        _realtimeAttendanceService = realtimeAttendanceService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _realtimeAttendanceService.GetLatestAsync(cancellationToken);
            return View(new HomeRealtimeAttendanceViewModel { Items = items });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible cargar los últimos registros de asistencia.");
            return View(new HomeRealtimeAttendanceViewModel());
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
