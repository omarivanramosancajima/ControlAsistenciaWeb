using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

public class ReubicacionEmpleadoController : Controller
{
    private readonly IReubicacionEmpleadoRepository _repository;

    public ReubicacionEmpleadoController(IReubicacionEmpleadoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var departments = await _repository.GetDepartmentsHierarchyAsync();
            return View(new ReubicacionEmpleadoIndexViewModel { Departments = departments });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla.";
            return View(new ReubicacionEmpleadoIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosPorDepartamento(
        int deptId, bool includeSubDependencies = false)
    {
        try
        {
            if (!await _repository.DepartmentExistsAsync(deptId))
                return NotFound(new { success = false, message = "El departamento seleccionado no existe." });

            var items = await _repository.GetEmployeesByDepartmentAsync(
                deptId, includeSubDependencies);

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
            return BadRequest(new
            {
                success = false,
                message = "No fue posible cargar las personas del área seleccionada."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> AreasDisponibles()
    {
        try
        {
            var items = await _repository.GetDepartmentsHierarchyAsync();
            return Json(new
            {
                success = true,
                items = items.Select(x => new
                {
                    deptId = x.DeptId,
                    deptName = x.DeptName,
                    supDeptId = x.SupDeptId,
                    level = x.Level,
                    hierarchyName = x.HierarchyName
                })
            });
        }
        catch
        {
            return BadRequest(new
            {
                success = false,
                message = "No fue posible cargar las áreas disponibles."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PersonasSeleccionadas([FromQuery] int[] userIds)
    {
        if (userIds is null || userIds.Length == 0)
            return Json(new { success = true, items = Array.Empty<object>() });

        try
        {
            var items = await _repository.GetEmployeesByUserIdsAsync(userIds.Distinct().ToArray());
            return Json(new
            {
                success = true,
                items = items.Select(x => new
                {
                    userId = x.UserId,
                    badgeNumber = x.BadgeNumber,
                    name = x.Name,
                    departmentName = x.DepartmentName
                })
            });
        }
        catch
        {
            return BadRequest(new
            {
                success = false,
                message = "No fue posible cargar las personas seleccionadas."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrasladarPersonal(
        [FromBody] ReubicacionEmpleadoTransferRequest request)
    {
        if (request.UserIds is null || request.UserIds.Count == 0)
            return BadRequest(new { success = false, message = "Debe seleccionar al menos una persona." });

        if (request.TargetDeptId <= 0)
            return BadRequest(new { success = false, message = "Debe seleccionar un área de destino." });

        if (request.TargetDeptId > short.MaxValue)
            return BadRequest(new { success = false, message = "El área seleccionada no es válida para USERINFO.DEFAULTDEPTID." });

        if (!await _repository.DepartmentExistsAsync(request.TargetDeptId))
            return BadRequest(new { success = false, message = "El área seleccionada no existe." });

        var userIds = request.UserIds.Distinct().ToList();
        var progress = new List<ReubicacionEmpleadoProgressItemViewModel>();
        var total = userIds.Count;
        var position = 0;

        foreach (var userId in userIds)
        {
            position++;
            if (userId <= 0)
            {
                progress.Add(new ReubicacionEmpleadoProgressItemViewModel
                {
                    Position = position,
                    Total = total,
                    UserId = userId,
                    Status = "Error",
                    Result = "Identificador de persona no válido.",
                    Success = false
                });
                continue;
            }

            var item = await _repository.TransferEmployeeAsync(userId, request.TargetDeptId);
            item.Position = position;
            item.Total = total;
            progress.Add(item);
        }

        return Json(new
        {
            success = progress.All(x => x.Status is "Completado" or "Sin cambio"),
            items = progress
        });
    }
}
