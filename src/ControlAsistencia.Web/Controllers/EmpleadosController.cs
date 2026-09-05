using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class EmpleadosController : Controller
{
    private const int PageSize = 10;
    private readonly IEmpleadoRepository _empleadoRepository;

    public EmpleadosController(IEmpleadoRepository empleadoRepository)
    {
        _empleadoRepository = empleadoRepository;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var currentPage = page <= 0 ? 1 : page;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        try
        {
            var result = await _empleadoRepository.GetPagedAsync(currentPage, PageSize, search);

            if (result.Items.Count > 0)
            {
                await _empleadoRepository.RegisterViewAuditAsync(result.Items[0].Name ?? string.Empty, GetOperatorName(), GetMachineAlias());
            }

            var model = new EmpleadoIndexViewModel
            {
                Empleados = result.Items,
                PageNumber = currentPage,
                PageSize = PageSize,
                TotalRecords = result.TotalRecords,
                Search = search
            };

            return View(model);
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar el listado de empleados.";
            return View(new EmpleadoIndexViewModel { PageNumber = 1, PageSize = PageSize });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await BuildFormAsync(new EmpleadoFormViewModel());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmpleadoFormViewModel model)
    {
        await ValidateFormAsync(model, null);

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var photoBytes = await ReadPhotoAsync(model.PhotoFile);
        var result = await _empleadoRepository.CreateAsync(model, photoBytes, GetOperatorName(), GetMachineAlias());

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(await BuildFormAsync(model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id);
        if (empleado is null)
        {
            TempData["ErrorMessage"] = "No se encontró el empleado solicitado.";
            return RedirectToAction(nameof(Index));
        }

        var model = await BuildFormAsync(new EmpleadoFormViewModel
        {
            UserId = empleado.UserId,
            BadgeNumber = empleado.BadgeNumber,
            Ssn = empleado.Ssn ?? string.Empty,
            Name = empleado.Name ?? string.Empty,
            Gender = empleado.Gender ?? string.Empty,
            Title = empleado.Title,
            Pager = empleado.Pager,
            Birthday = empleado.Birthday,
            HiredDay = empleado.HiredDay,
            Street = empleado.Street,
            OPhone = empleado.OPhone,
            DefaultDeptId = empleado.DefaultDeptId,
            DepartmentName = empleado.DepartmentName,
            Minzu = empleado.Minzu,
            MVerifyPass = empleado.MVerifyPass,
            Privilege = empleado.Privilege,
            CardNo = empleado.CardNo,
            CurrentPhoto = empleado.Photo,
            CurrentPhotoBase64 = empleado.PhotoBase64
        });

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmpleadoFormViewModel model)
    {
        await ValidateFormAsync(model, model.UserId);

        var actual = await _empleadoRepository.GetByIdAsync(model.UserId);
        if (actual is null)
        {
            TempData["ErrorMessage"] = "No se encontró el empleado solicitado.";
            return RedirectToAction(nameof(Index));
        }

        model.CurrentPhoto = actual.Photo;
        model.CurrentPhotoBase64 = actual.PhotoBase64;

        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        var photoBytes = await ReadPhotoAsync(model.PhotoFile);
        var result = await _empleadoRepository.UpdateAsync(model, photoBytes, GetOperatorName(), GetMachineAlias());

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(await BuildFormAsync(model));
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id);
        if (empleado is null)
        {
            TempData["ErrorMessage"] = "No se encontró el empleado solicitado.";
            return RedirectToAction(nameof(Index));
        }

        return View(new DeleteEmpleadoViewModel { Empleado = empleado });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int userId)
    {
        var dependency = await _empleadoRepository.ValidateDeleteAsync(userId);
        if (dependency.HasDependency)
        {
            TempData["ErrorMessage"] = dependency.DependencyMessage;
            return RedirectToAction(nameof(Delete), new { id = userId });
        }

        var result = await _empleadoRepository.DeleteAsync(userId, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DepartmentsPopup()
    {
        var departments = await _empleadoRepository.GetDepartmentsHierarchyAsync();
        return PartialView("_DepartmentsPopup", departments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportPhoto(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return Json(new
            {
                success = false,
                status = "error",
                message = "No se recibió una imagen."
            });
        }

        var extension = Path.GetExtension(file.FileName);
        if (!new[] { ".jpg", ".jpeg", ".gif" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Json(new
            {
                success = false,
                status = "error",
                message = "Formato de imagen no compatible."
            });
        }

        const long maxPhotoSize = 60 * 1024;
        if (file.Length > maxPhotoSize)
        {
            return Json(new
            {
                success = false,
                status = "oversize",
                message = "La imagen supera el tamaño máximo permitido de 60 KB."
            });
        }

        var badgeNumber = Path.GetFileNameWithoutExtension(file.FileName)?.Trim();
        if (string.IsNullOrWhiteSpace(badgeNumber))
        {
            return Json(new
            {
                success = false,
                status = "notfound",
                message = "No fue posible obtener el código del empleado desde el nombre del archivo."
            });
        }

        try
        {
            var photoBytes = await ReadPhotoAsync(file);
            var result = await _empleadoRepository.UpdatePhotoByBadgeNumberAsync(
                badgeNumber,
                photoBytes,
                GetOperatorName(),
                GetMachineAlias());

            return Json(new
            {
                success = result.Success,
                status = !result.Found ? "notfound" : (result.Success ? "loaded" : "error"),
                message = result.Message
            });
        }
        catch
        {
            return Json(new
            {
                success = false,
                status = "error",
                message = "No fue posible procesar la imagen."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string deptName, int supDeptId)
    {
        if (string.IsNullOrWhiteSpace(deptName))
        {
            return Json(new { success = false, message = "El nombre del área es obligatorio." });
        }

        var result = await _empleadoRepository.CreateDepartmentAsync(deptName.Trim(), supDeptId);
        if (!result.Success)
        {
            return Json(new { success = false, message = result.Message });
        }

        var departments = await _empleadoRepository.GetDepartmentsHierarchyAsync();
        return Json(new { success = true, message = result.Message, departments });
    }

    private async Task<EmpleadoFormViewModel> BuildFormAsync(EmpleadoFormViewModel model)
    {
        model.GenderOptions = new[]
        {
            new SelectListItem("Masculino", "M"),
            new SelectListItem("Femenino", "F")
        };

        model.PrivilegeOptions = new[]
        {
            new SelectListItem("Inválido", "-1"),
            new SelectListItem("Usuario", "0"),
            new SelectListItem("Enrolar", "1"),
            new SelectListItem("Administrador", "2"),
            new SelectListItem("Supervisor", "3")
        };

        if (model.DefaultDeptId.HasValue && string.IsNullOrWhiteSpace(model.DepartmentName))
        {
            var departments = await _empleadoRepository.GetDepartmentsHierarchyAsync();
            model.DepartmentName = departments.FirstOrDefault(x => x.DeptId == model.DefaultDeptId)?.HierarchyName;
        }

        return model;
    }

    private async Task ValidateFormAsync(EmpleadoFormViewModel model, int? excludeUserId)
    {
        if (!string.IsNullOrWhiteSpace(model.BadgeNumber) && await _empleadoRepository.ExistsBadgeNumberAsync(model.BadgeNumber.Trim(), excludeUserId))
        {
            ModelState.AddModelError(nameof(model.BadgeNumber), "El código ya se encuentra registrado.");
        }

        if (model.DefaultDeptId.HasValue && !await _empleadoRepository.DepartmentExistsAsync(model.DefaultDeptId.Value))
        {
            ModelState.AddModelError(nameof(model.DefaultDeptId), "El área seleccionada no es válida.");
        }
    }

    private static async Task<byte[]?> ReadPhotoAsync(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return null;
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;
}