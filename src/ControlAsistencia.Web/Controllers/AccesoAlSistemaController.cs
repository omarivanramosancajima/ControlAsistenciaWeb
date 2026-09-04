using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class AccesoAlSistemaController : Controller
{
    private const int PageSize = 10;
    private readonly IAccesoAlSistemaRepository _repository;

    public AccesoAlSistemaController(IAccesoAlSistemaRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var currentPage = page <= 0 ? 1 : page;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        try
        {
            var result = await _repository.GetPagedAsync(currentPage, PageSize, search);
            await _repository.RegisterViewAuditAsync(GetOperatorName(), GetMachineAlias());

            return View(new AccesoAlSistemaIndexViewModel
            {
                Items = result.Items,
                PageNumber = currentPage,
                PageSize = PageSize,
                TotalRecords = result.TotalRecords,
                Search = search
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar el listado de accesos al sistema.";
            return View(new AccesoAlSistemaIndexViewModel
            {
                PageNumber = currentPage,
                PageSize = PageSize,
                Search = search
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Nuevo(int? deptId, int? userId)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        try
        {
            var departments = await _repository.GetDepartmentsHierarchyAsync();
            if (departments.Count == 0)
            {
                TempData["ErrorMessage"] = "No existen áreas registradas para seleccionar personal.";
                return RedirectToAction(nameof(Index));
            }

            var selectedDeptId = deptId.GetValueOrDefault();
            if (selectedDeptId == 0 || departments.All(x => x.DeptId != selectedDeptId))
                selectedDeptId = departments.First(x => x.Level == 0).DeptId;

            var employees = await _repository.GetEmployeesWithoutAccessByDepartmentAsync(selectedDeptId);
            AccesoAlSistemaEmployeeItemViewModel? selectedEmployee = null;

            if (userId.HasValue)
                selectedEmployee = employees.FirstOrDefault(x => x.UserId == userId.Value)
                    ?? await _repository.GetEmployeeWithoutAccessAsync(userId.Value);

            return View(new AccesoAlSistemaNewViewModel
            {
                Departments = departments,
                Employees = employees,
                SelectedDeptId = selectedDeptId,
                SelectedUserId = selectedEmployee?.UserId,
                SelectedEmployee = selectedEmployee
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla de nuevo acceso.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(AccesoAlSistemaNewViewModel model, short securityFlags)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        if (!IsValidAccessType(securityFlags))
        {
            ViewBag.ErrorMessage = "Debe seleccionar un tipo de acceso válido.";
            return await ReturnNewViewAsync(model);
        }

        if (!model.SelectedUserId.HasValue)
        {
            ViewBag.ErrorMessage = "Debe seleccionar una persona.";
            return await ReturnNewViewAsync(model);
        }

        var employee = await _repository.GetEmployeeWithoutAccessAsync(model.SelectedUserId.Value);
        if (employee is null)
        {
            ViewBag.ErrorMessage = "La persona seleccionada ya tiene acceso al sistema o no existe.";
            return await ReturnNewViewAsync(model);
        }

        var result = await _repository.CreateAccessAsync(
            employee.UserId, securityFlags, GetOperatorName(), GetMachineAlias());

        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            model.SelectedUserId = employee.UserId;
            model.SelectedEmployee = employee;
            return await ReturnNewViewAsync(model);
        }

        TempData["SuccessMessage"] = $"Acceso creado correctamente para {employee.Name ?? employee.BadgeNumber}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "No se encontró el acceso solicitado.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        return View(new AccesoAlSistemaEditViewModel
        {
            UserId = item.UserId,
            BadgeNumber = item.BadgeNumber,
            Ssn = item.Ssn,
            Name = item.Name,
            DepartmentName = item.DepartmentName,
            Photo = item.Photo,
            PhotoBase64 = item.PhotoBase64,
            SecurityFlags = item.SecurityFlags ?? 0,
            NewSecurityFlags = item.SecurityFlags ?? 0,
            AccessDescription = item.AccessDescription
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        AccesoAlSistemaEditViewModel model, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetByIdAsync(model.UserId);
        if (item is null)
        {
            TempData["ErrorMessage"] = "No se encontró el acceso solicitado.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        model.BadgeNumber = item.BadgeNumber;
        model.Ssn = item.Ssn;
        model.Name = item.Name;
        model.DepartmentName = item.DepartmentName;
        model.Photo = item.Photo;
        model.PhotoBase64 = item.PhotoBase64;
        model.SecurityFlags = item.SecurityFlags ?? 0;
        model.AccessDescription = item.AccessDescription;

        if (!IsValidAccessType(model.NewSecurityFlags))
            ModelState.AddModelError(nameof(model.NewSecurityFlags), "Debe seleccionar un tipo de acceso válido.");

        if (model.NewSecurityFlags == model.SecurityFlags)
            ModelState.AddModelError(nameof(model.NewSecurityFlags), "Debe seleccionar un nivel de acceso diferente al actual.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _repository.UpdateAccessAsync(
            model.UserId, model.NewSecurityFlags, GetOperatorName(), GetMachineAlias());

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Acceso actualizado correctamente para {model.Name ?? model.BadgeNumber}.";
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "No se encontró el acceso solicitado.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        return View(new AccesoAlSistemaDeleteViewModel
        {
            UserId = item.UserId,
            BadgeNumber = item.BadgeNumber,
            Ssn = item.Ssn,
            Name = item.Name,
            DepartmentName = item.DepartmentName,
            Photo = item.Photo,
            PhotoBase64 = item.PhotoBase64,
            SecurityFlags = item.SecurityFlags ?? 0,
            AccessDescription = item.AccessDescription
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(
        AccesoAlSistemaDeleteViewModel model, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetByIdAsync(model.UserId);
        if (item is null)
        {
            TempData["ErrorMessage"] = "El acceso ya no existe o ya fue eliminado.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        var result = await _repository.DeleteAccessAsync(
            model.UserId, GetOperatorName(), GetMachineAlias());

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? $"Acceso eliminado correctamente para {item.Name ?? item.BadgeNumber}."
            : result.Message;

        return RedirectToAction(nameof(Index), new { search, page });
    }

    private async Task<IActionResult> ReturnNewViewAsync(AccesoAlSistemaNewViewModel model)
    {
        var departments = await _repository.GetDepartmentsHierarchyAsync();
        var selectedDeptId = model.SelectedDeptId;

        if (selectedDeptId == 0 || departments.All(x => x.DeptId != selectedDeptId))
            selectedDeptId = departments.FirstOrDefault(x => x.Level == 0)?.DeptId ?? 0;

        var employees = selectedDeptId == 0
            ? Array.Empty<AccesoAlSistemaEmployeeItemViewModel>()
            : await _repository.GetEmployeesWithoutAccessByDepartmentAsync(selectedDeptId);

        AccesoAlSistemaEmployeeItemViewModel? selectedEmployee = null;
        if (model.SelectedUserId.HasValue)
            selectedEmployee = await _repository.GetEmployeeWithoutAccessAsync(model.SelectedUserId.Value);

        model.Departments = departments;
        model.Employees = employees;
        model.SelectedDeptId = selectedDeptId;
        model.SelectedEmployee = selectedEmployee;
        return View("Nuevo", model);
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) ||
            !await _repository.CanManageAccessAsync(userId))
        {
            TempData["ErrorMessage"] = "No tiene permisos para acceder a Acceso al Sistema.";
            return false;
        }

        return true;
    }

    private static bool IsValidAccessType(short value) =>
        value is 15 or 7 or 8 or 9 or 5;

    private string GetOperatorName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;
}
