using System.Globalization;
using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class MarcacionEmpleadoController : Controller
{
    private readonly IMarcacionEmpleadoRepository _repository;

    public MarcacionEmpleadoController(IMarcacionEmpleadoRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(new MarcacionEmpleadoIndexViewModel
            {
                Departments = await _repository.GetDepartmentsHierarchyAsync()
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla Marcación del Empleado.";
            return View(new MarcacionEmpleadoIndexViewModel());
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
            return Json(new { success = true, items = items.Select(MapEmployee) });
        }
        catch
        {
            return BadRequest(new { success = false, message = "No fue posible cargar las personas del área seleccionada." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MarcacionesPorPersona(int userId, int page = 1)
    {
        try
        {
            if (!await _repository.UserExistsAsync(userId))
                return NotFound(new { success = false, message = "La persona seleccionada no existe." });

            page = page <= 0 ? 1 : page;
            const int pageSize = 35;
            var result = await _repository.GetMarcacionesByUserAsync(userId, page, pageSize);

            return Json(new
            {
                success = true,
                page,
                pageSize,
                hasPreviousPage = page > 1,
                hasNextPage = result.HasNextPage,
                items = result.Items.Select(x => new
                {
                    userId = x.UserId,
                    checkTime = x.CheckTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    checkType = x.CheckType,
                    verifyCode = x.VerifyCode,
                    sensorId = x.SensorId,
                    memoInfo = x.MemoInfo,
                    workCode = x.WorkCode,
                    serialNumber = x.SerialNumber,
                    userExtFmt = x.UserExtFmt,
                    isAdd = x.IsAdd,
                    reason = x.Reason,
                    modifiedBy = x.ModifiedBy,
                    registeredAt = x.RegisteredAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    canDelete = x.CanDelete,
                    recordType = x.RecordType
                })
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"No fue posible cargar las marcaciones de la persona seleccionada: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarMarca([FromBody] MarcacionEmpleadoRegisterRequest request)
    {
        if (!Request.HasJsonContentType())
            return BadRequest(new { success = false, message = "Formato de petición no válido." });

        var userIds = request.UserIds?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (userIds.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar al menos una persona." });

        if (!request.CheckDate.HasValue)
            return BadRequest(new { success = false, message = "Debe ingresar la fecha de marcaje." });

        if (!TimeSpan.TryParseExact(request.CheckTime, @"hh\:mm", CultureInfo.InvariantCulture, out var time))
            return BadRequest(new { success = false, message = "Debe ingresar la hora de marcaje en formato HH:mm." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { success = false, message = "Debe ingresar el motivo." });

        if (request.Reason.Length > 25)
            return BadRequest(new { success = false, message = "El motivo no puede superar 25 caracteres." });

        var operatorName = GetOperatorName();
        if (operatorName.Length > 20)
            return BadRequest(new { success = false, message = "El usuario operador supera los 20 caracteres permitidos por CHECKEXACT.MODIFYBY." });

        request.UserIds = userIds;
        request.CheckTime = time.ToString(@"hh\:mm");

        try
        {
            var result = await _repository.RegisterManualMarkAsync(
                request, operatorName, GetMachineAlias());

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
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = $"Error al registrar la marca: {ex.Message}"
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarEliminar([FromBody] MarcacionEmpleadoDeleteRequest request)
    {
        if (request.UserId <= 0 || request.Items is null || request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar una o varias marcaciones para borrar." });

        if (!await _repository.UserExistsAsync(request.UserId))
            return BadRequest(new { success = false, message = "La persona seleccionada no existe." });

        var model = await _repository.GetDeleteConfirmationAsync(request.UserId, request.Items);
        if (model is null || model.Items.Count != request.Items.Count)
            return BadRequest(new { success = false, message = "Una o varias marcaciones ya no están disponibles para borrar." });

        return PartialView("_ConfirmDeleteMarcacionesModal", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMarcaciones([FromBody] MarcacionEmpleadoDeleteRequest request)
    {
        if (!Request.HasJsonContentType())
            return BadRequest(new { success = false, message = "Formato de petición no válido." });

        if (request.UserId <= 0 || !await _repository.UserExistsAsync(request.UserId))
            return BadRequest(new { success = false, message = "Debe seleccionar una persona." });

        request.Items = request.Items?
            .GroupBy(x => x.CheckTime)
            .Select(g => g.First())
            .ToList() ?? [];

        if (request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar una o varias marcaciones para borrar." });

        var result = await _repository.DeleteMarcacionesAsync(
            request, GetOperatorName(), GetMachineAlias());

        return Json(new { success = result.Success, message = result.Message });
    }

    private static object MapEmployee(MarcacionEmpleadoEmployeeItemViewModel x) => new
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
    };

    private string GetOperatorName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;
}