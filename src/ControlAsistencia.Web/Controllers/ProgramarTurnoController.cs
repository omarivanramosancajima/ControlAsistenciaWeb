using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class ProgramarTurnoController : Controller
{
    private readonly ITurnoRepository _repository;

    public ProgramarTurnoController(ITurnoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? turnoId = null)
    {
        try
        {
            var turnos = await _repository.GetAllForScheduleAssignmentAsync();
            TurnoAsignacionDetalleViewModel? selectedTurno = null;

            if (turnoId.HasValue)
            {
                selectedTurno = await BuildTurnoDetailAsync(turnoId.Value);
            }

            return View(new ProgramarTurnoIndexViewModel
            {
                Turnos = turnos,
                SelectedTurnoId = selectedTurno?.NumRunId,
                SelectedTurno = selectedTurno
            });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla de asignación de horarios.";
            return View(new ProgramarTurnoIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> TurnoDetalle(int turnoId)
    {
        try
        {
            var detail = await BuildTurnoDetailAsync(turnoId);
            if (detail is null)
            {
                return NotFound();
            }

            return PartialView("_TurnoDetalle", detail);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> AsignarHorarioModal(int turnoId)
    {
        try
        {
            var turno = await _repository.GetByIdAsync(turnoId);
            if (turno is null)
            {
                return NotFound();
            }

            var assignments = await _repository.GetAsignacionesPorTurnoAsync(turnoId);
            var horarios = await _repository.GetHorariosForScheduleAssignmentAsync();
            var units = turno.UNITS ?? -1;
            var cyle = turno.CYLE ?? 0;

            var model = new ProgramarTurnoModalViewModel
            {
                NumRunId = turno.NUM_RUNID,
                NombreTurno = turno.NAME,
                Units = units,
                Cyle = cyle,
                FrecuenciaTexto = TurnoCycleDayHelper.GetFrequencyLabel(units),
                Horarios = horarios.Select(x => new HorarioAsignacionItemViewModel
                {
                    SchClassId = x.SchClassid,
                    SchName = x.SchName,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Color = x.Color
                }).ToList(),
                Dias = TurnoCycleDayHelper.BuildDays(units, cyle, assignments)
            };

            return PartialView("_AsignarHorarioModal", model);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarAsignaciones([FromBody] GuardarProgramacionTurnoRequest request)
    {
        if (!Request.HasJsonContentType())
        {
            return BadRequest(new { success = false, message = "Formato de petición no válido." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Los datos enviados no son válidos." });
        }

        if ((request.SelectedDays?.Count ?? 0) == 0 && (request.UnselectedDays?.Count ?? 0) == 0)
        {
            return BadRequest(new { success = false, message = "Debe existir al menos una modificación para guardar." });
        }

        var result = await _repository.SaveScheduleAssignmentsAsync(request, GetOperatorName(), GetMachineAlias());
        return Json(new { success = result.Success, message = result.Message });
    }

    private async Task<TurnoAsignacionDetalleViewModel?> BuildTurnoDetailAsync(int turnoId)
    {
        var turno = await _repository.GetByIdAsync(turnoId);
        if (turno is null)
        {
            return null;
        }

        var units = turno.UNITS ?? -1;
        var cyle = turno.CYLE ?? 0;
        var assignments = await _repository.GetAsignacionesPorTurnoAsync(turnoId);

        return new TurnoAsignacionDetalleViewModel
        {
            NumRunId = turno.NUM_RUNID,
            NombreTurno = turno.NAME,
            Units = units,
            Cyle = cyle,
            FrecuenciaTexto = TurnoCycleDayHelper.GetFrequencyLabel(units),
            Dias = TurnoCycleDayHelper.BuildDays(units, cyle, assignments)
        };
    }

    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";
    private string GetMachineAlias() => Environment.MachineName;
}