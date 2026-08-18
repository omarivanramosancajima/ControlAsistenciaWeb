using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class TurnosdelEmpleadoController : Controller
{
    private readonly ITurnosEmpleadoRepository _repository;

    public TurnosdelEmpleadoController(ITurnosEmpleadoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var departments = await _repository.GetDepartmentsHierarchyAsync();
            var turnos = await _repository.GetTurnosAsync();

            return View(new TurnosEmpleadoIndexViewModel
            {
                Departments = departments,
                Turnos = turnos
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla de programación de empleados.";
            return View(new TurnosEmpleadoIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosPorDepartamento(int deptId, bool includeSubDependencies = false)
    {
        try
        {
            if (!await _repository.DepartmentExistsAsync(deptId))
            {
                return NotFound(new { success = false, message = "El departamento seleccionado no existe." });
            }

            var items = await _repository.GetEmployeesByDepartmentAsync(deptId, includeSubDependencies);
            var data = items.Select(x => new
            {
                userId = x.UserId,
                badgeNumber = x.BadgeNumber,
                ssn = x.Ssn,
                name = x.Name,
                defaultDeptId = x.DefaultDeptId,
                departmentName = x.DepartmentName,
                photoBase64 = x.PhotoBase64,
                privilege = x.Privilege,
                privilegeDescription = x.PrivilegeDescription
            });

            return Json(new { success = true, items = data });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las personas del área seleccionada." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AsignacionesPorPersona(int userId)
    {
        try
        {
            if (!await _repository.UserExistsAsync(userId))
            {
                return NotFound(new { success = false, message = "La persona seleccionada no existe." });
            }

            var items = await _repository.GetAsignacionesByUserAsync(userId);
            var data = items.Select(x => new
            {
                userId = x.UserId,
                numOfRunId = x.NumOfRunId,
                startDate = x.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                endDate = x.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                turnoName = x.TurnoName,
                frequencyText = x.FrequencyText,
                cyle = x.Cyle,
                turnoStartDate = x.TurnoStartDate?.ToString("dd/MM/yyyy"),
                turnoEndDate = x.TurnoEndDate?.ToString("dd/MM/yyyy")
            });

            return Json(new { success = true, items = data });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las asignaciones de la persona seleccionada." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> TurnosDisponibles()
    {
        try
        {
            var items = await _repository.GetTurnosAsync();
            var data = items.Select(x => new
            {
                numRunId = x.NUM_RUNID,
                name = x.NAME,
                startDate = x.STARTDATE?.ToString("dd/MM/yyyy"),
                endDate = x.ENDDATE?.ToString("dd/MM/yyyy"),
                cyle = x.CYLE,
                units = x.UNITS,
                frequencyText = TurnoCycleDayHelper.GetFrequencyLabel(x.UNITS ?? -1)
            });

            return Json(new { success = true, items = data });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar los turnos disponibles." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarTurno([FromBody] TurnosEmpleadoAssignRequest request)
    {
        if (!Request.HasJsonContentType())
        {
            return BadRequest(new { success = false, message = "Formato de petición no válido." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Los datos enviados no son válidos." });
        }

        var distinctUserIds = request.UserIds?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (distinctUserIds.Count == 0)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar al menos una persona." });
        }

        if (!request.NumRunId.HasValue || request.NumRunId.Value <= 0)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar un turno." });
        }

        if (!request.StartDate.HasValue || !request.EndDate.HasValue)
        {
            return BadRequest(new { success = false, message = "Debe ingresar la fecha de inicio y fin." });
        }

        if (request.StartDate.Value.Date > request.EndDate.Value.Date)
        {
            return BadRequest(new { success = false, message = "La fecha de inicio no puede ser mayor a la fecha fin." });
        }

        var turno = await _repository.GetTurnoByIdAsync(request.NumRunId.Value);

        if (turno is null)
        {
            return BadRequest(new
            {
                success = false,
                message = "No fue posible obtener el rango de fechas del turno."
            });
        }

        var turnoStartDate = turno.StartDate?.Date;
        var turnoEndDate = turno.EndDate?.Date;

        if (!turnoStartDate.HasValue || !turnoEndDate.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "El turno seleccionado no tiene configurado un rango de fechas válido."
            });
        }

        if (request.StartDate.Value.Date < turnoStartDate.Value ||
            request.EndDate.Value.Date > turnoEndDate.Value)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    $"El rango de asignación del empleado debe estar dentro del rango del turno " +
                    $"({turnoStartDate:dd/MM/yyyy} - {turnoEndDate:dd/MM/yyyy})."
            });
        }

        if (!await _repository.TurnoExistsAsync(request.NumRunId.Value))
        {
            return BadRequest(new { success = false, message = "El turno seleccionado no existe." });
        }

        foreach (var userId in distinctUserIds)
        {
            if (!await _repository.UserExistsAsync(userId))
            {
                return BadRequest(new { success = false, message = $"La persona con identificador {userId} no existe." });
            }
        }

        request.UserIds = distinctUserIds;

        var result = await _repository.AssignTurnoAsync(request, GetOperatorName(), GetMachineAlias());
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            progressItems = result.ProgressItems.Select(x => new
            {
                position = x.Position,
                total = x.Total,
                userId = x.UserId,
                employeeName = x.EmployeeName,
                badgeNumber = x.BadgeNumber,
                status = x.Status,
                result = x.Result,
                success = x.Success
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmarEliminar(int userId, int numOfRunId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var asignacion = await _repository.GetAsignacionByKeyAsync(userId, numOfRunId, startDate, endDate);
            if (asignacion is null)
            {
                return NotFound();
            }

            var empleado = await _repository.GetEmployeeByIdAsync(userId);

            var model = new TurnosEmpleadoDeleteConfirmViewModel
            {
                UserId = userId,
                Ssn = empleado?.Ssn,
                EmployeeName = empleado?.Name ?? string.Empty,
                NumOfRunId = numOfRunId,
                TurnoName = asignacion.TurnoName,
                StartDate = asignacion.StartDate,
                EndDate = asignacion.EndDate
            };

            return PartialView("_ConfirmDeleteAsignacionModal", model);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAsignacion([FromBody] TurnosEmpleadoDeleteRequest request)
    {
        if (!Request.HasJsonContentType())
        {
            return BadRequest(new { success = false, message = "Formato de petición no válido." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Los datos enviados no son válidos." });
        }

        if (!request.UserId.HasValue || !await _repository.UserExistsAsync(request.UserId.Value))
        {
            return BadRequest(new { success = false, message = "Debe seleccionar una persona." });
        }

        if (!request.NumOfRunId.HasValue || request.NumOfRunId.Value <= 0 || !request.StartDate.HasValue || !request.EndDate.HasValue)
        {
            return BadRequest(new { success = false, message = "Debe seleccionar una asignación de turno." });
        }

        var existing = await _repository.GetAsignacionByKeyAsync(request.UserId.Value, request.NumOfRunId.Value, request.StartDate.Value, request.EndDate.Value);
        if (existing is null)
        {
            return BadRequest(new { success = false, message = "La asignación seleccionada ya no existe." });
        }

        var result = await _repository.DeleteAsignacionAsync(request, GetOperatorName(), GetMachineAlias());
        return Json(new { success = result.Success, message = result.Message });
    }

    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";
    private string GetMachineAlias() => Environment.MachineName;
}