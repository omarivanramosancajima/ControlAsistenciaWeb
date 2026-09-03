using System.Globalization;
using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class JustificarEmpleadoController : Controller
{
    private readonly IJustificarEmpleadoRepository _repository;

    public JustificarEmpleadoController(IJustificarEmpleadoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var departments = await _repository.GetDepartmentsHierarchyAsync();
            var exceptions = await _repository.GetExcepcionesAsync();

            return View(new JustificarEmpleadoIndexViewModel
            {
                Departments = departments,
                Excepciones = exceptions
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla Justificar Empleado.";
            return View(new JustificarEmpleadoIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosPorDepartamento(int deptId, bool includeSubDependencies = false)
    {
        try
        {
            if (!await _repository.DepartmentExistsAsync(deptId))
                return NotFound(new { success = false, message = "El departamento seleccionado no existe." });

            var items = await _repository.GetEmployeesByDepartmentAsync(deptId, includeSubDependencies);
            return Json(new
            {
                success = true,
                items = items.Select(x => new
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
                })
            });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las personas del área seleccionada." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExcepcionesPorPersona(int userId)
    {
        try
        {
            if (!await _repository.UserExistsAsync(userId))
                return NotFound(new { success = false, message = "La persona seleccionada no existe." });

            var items = await _repository.GetAsignacionesByUserAsync(userId);

            return Json(new
            {
                success = true,
                items = items.Select(x => new
                {
                    userId = x.UserId,
                    leaveId = x.LeaveId,
                    leaveName = x.LeaveName,
                    startDateTime = x.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    endDateTime = x.EndDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    reason = x.Reason,
                    unit = x.Unit,
                    unitText = x.UnitText,
                    reportSymbol = x.ReportSymbol,
                    classify = x.Classify,
                    classifyText = x.ClassifyText,
                    registeredAt = x.RegisteredAt?.ToString("yyyy-MM-ddTHH:mm:ss")
                })
            });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las asignaciones de la persona seleccionada." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExcepcionesDisponibles()
    {
        try
        {
            var items = await _repository.GetExcepcionesAsync();
            return Json(new
            {
                success = true,
                items = items.Select(x => new
                {
                    leaveId = x.LeaveId,
                    leaveName = x.LeaveName,
                    unit = x.Unit,
                    unitText = x.UnitText,
                    classify = x.Classify,
                    classifyText = x.ClassifyText
                })
            });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las excepciones disponibles." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarExcepcion([FromBody] JustificarEmpleadoAssignRequest request)
    {
        if (!Request.HasJsonContentType())
            return BadRequest(new { success = false, message = "Formato de petición no válido." });

        var userIds = request.UserIds?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (userIds.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar al menos una persona." });

        if (request.LeaveId <= 0 || !await _repository.ExcepcionExistsAsync(request.LeaveId))
            return BadRequest(new { success = false, message = "Debe seleccionar una excepción válida." });

        if (!request.StartDate.HasValue || !request.EndDate.HasValue)
            return BadRequest(new { success = false, message = "Debe ingresar la fecha de inicio y la fecha de fin." });

        var startDate = request.StartDate.Value.Date;
        var endDate = request.EndDate.Value.Date;

        if (startDate > endDate)
            return BadRequest(new { success = false, message = "La fecha de inicio no puede ser mayor que la fecha fin." });

        var exception = await _repository.GetExcepcionByIdAsync(request.LeaveId);
        if (exception is null)
            return BadRequest(new { success = false, message = "La excepción seleccionada ya no existe." });

        if (exception.Unit is 1 or 2)
        {
            if (!TimeSpan.TryParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out _) ||
                !TimeSpan.TryParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out _))
            {
                return BadRequest(new { success = false, message = "Debe ingresar correctamente la hora de inicio y la hora de fin en formato HH:mm." });
            }

            if (string.IsNullOrWhiteSpace(request.StartTime) || string.IsNullOrWhiteSpace(request.EndTime))
                return BadRequest(new { success = false, message = "Debe ingresar la hora de inicio y la hora de fin." });

            if (TimeSpan.ParseExact(request.StartTime, @"hh\:mm", CultureInfo.InvariantCulture) >
                TimeSpan.ParseExact(request.EndTime, @"hh\:mm", CultureInfo.InvariantCulture))
            {
                return BadRequest(new { success = false, message = "La hora de inicio no puede ser mayor que la hora fin." });
            }
        }
        else
        {
            request.StartTime = null;
            request.EndTime = null;
        }

        if (!string.IsNullOrEmpty(request.Reason) && request.Reason.Length > 200)
            return BadRequest(new { success = false, message = "El Motivo no puede superar 200 caracteres." });

        foreach (var userId in userIds)
        {
            if (!await _repository.UserExistsAsync(userId))
                return BadRequest(new { success = false, message = $"La persona con identificador {userId} no existe." });
        }

        request.UserIds = userIds;

        var result = await _repository.AssignExcepcionAsync(
            request,
            GetOperatorName(),
            GetMachineAlias());

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar([FromBody] JustificarEmpleadoDeleteRequest request)
    {
        if (request.UserId <= 0 || request.Items is null || request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar una o varias asignaciones para borrar." });

        if (!await _repository.UserExistsAsync(request.UserId))
            return BadRequest(new { success = false, message = "La persona seleccionada no existe." });

        var model = await _repository.GetDeleteConfirmationAsync(request.UserId, request.Items);
        if (model is null || model.Items.Count != request.Items.Count)
            return BadRequest(new { success = false, message = "Una o varias asignaciones seleccionadas ya no existen." });

        return PartialView("_ConfirmDeleteExcepcionesModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarExcepciones([FromBody] JustificarEmpleadoDeleteRequest request)
    {
        if (!Request.HasJsonContentType())
            return BadRequest(new { success = false, message = "Formato de petición no válido." });

        if (request.UserId <= 0 || !await _repository.UserExistsAsync(request.UserId))
            return BadRequest(new { success = false, message = "Debe seleccionar una persona." });

        request.Items = request.Items?
            .GroupBy(x => $"{x.LeaveId}|{x.StartDateTime:O}|{x.EndDateTime:O}")
            .Select(g => g.First())
            .ToList() ?? [];

        if (request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar una o varias asignaciones para borrar." });

        var result = await _repository.DeleteExcepcionesAsync(
            request,
            GetOperatorName(),
            GetMachineAlias());

        return Json(new { success = result.Success, message = result.Message });
    }

    private string GetOperatorName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;
}
