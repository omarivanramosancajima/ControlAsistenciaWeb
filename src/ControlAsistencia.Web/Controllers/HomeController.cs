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
    private readonly IHomeDailyAttendanceStatsService _dailyAttendanceStatsService;

    public HomeController(
        ILogger<HomeController> logger,
        RealtimeAttendanceService realtimeAttendanceService,
        IHomeDailyAttendanceStatsService dailyAttendanceStatsService)
    {
        _logger = logger;
        _realtimeAttendanceService = realtimeAttendanceService;
        _dailyAttendanceStatsService = dailyAttendanceStatsService;
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

    [HttpGet]
    public async Task<IActionResult> DailyAttendanceStats(
        CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _dailyAttendanceStatsService.GetTodayAsync(cancellationToken);
            return Json(stats);
        }
        catch (OperationCanceledException)
        {
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "No fue posible obtener los indicadores diarios de asistencia.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
