using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class EmpresasController : Controller
{
    private const int PageSize = 10;
    private readonly IEmpresaRepository _repository;
    private readonly ILogger<EmpresasController> _logger;

    public EmpresasController(
        IEmpresaRepository repository,
        ILogger<EmpresasController> logger)
    {
        _repository = repository;
        _logger = logger;
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

            return View(new EmpresaIndexViewModel
            {
                Items = result.Items,
                PageNumber = currentPage,
                PageSize = PageSize,
                TotalRecords = result.TotalRecords,
                Search = search
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar empresas.");
            TempData["ErrorMessage"] = "No fue posible cargar el listado de empresas.";
            return View(new EmpresaIndexViewModel
            {
                PageNumber = currentPage,
                PageSize = PageSize,
                Search = search
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        return View(new EmpresaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EmpresaFormViewModel model)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        Normalize(model);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _repository.CreateAsync(
                model, GetOperatorName(), GetMachineAlias());

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear empresa.");
            ModelState.AddModelError(string.Empty, "No fue posible crear la empresa.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetByIdAsync(id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "No se encontró la empresa solicitada.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        EmpresaFormViewModel model, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        Normalize(model);

        var current = await _repository.GetByIdAsync(model.CompanyId);
        if (current is null)
        {
            TempData["ErrorMessage"] = "La empresa no existe o ya no está activa.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        // RUC y razón social son de solo lectura.
        model.TaxId = current.TaxId;
        model.Descrip = current.Descrip;

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _repository.UpdateAsync(
                model, GetOperatorName(), GetMachineAlias());

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { search, page });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar empresa.");
            ModelState.AddModelError(string.Empty, "No fue posible actualizar la empresa.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        var item = await _repository.GetDeleteByIdAsync(id);
        if (item is null)
        {
            TempData["ErrorMessage"] = "No se encontró la empresa solicitada.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(
        int id, string? search, int page = 1)
    {
        if (!await EnsurePermissionAsync())
            return RedirectToAction("Index", "Home");

        try
        {
            var result = await _repository.DeleteAsync(
                id, GetOperatorName(), GetMachineAlias());

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { search, page });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar empresa.");
            TempData["ErrorMessage"] = "No fue posible eliminar la empresa.";
            return RedirectToAction(nameof(Index), new { search, page });
        }
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) ||
            !await _repository.CanManageAsync(userId))
        {
            TempData["ErrorMessage"] = "No tiene permisos para acceder a Empresas.";
            return false;
        }

        return true;
    }

    private string GetOperatorName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;

    private static void Normalize(EmpresaFormViewModel model)
    {
        model.TaxId = (model.TaxId ?? string.Empty).Trim();
        model.Descrip = (model.Descrip ?? string.Empty).Trim();
        model.Tel = string.IsNullOrWhiteSpace(model.Tel) ? null : model.Tel.Trim();
        model.Movil = string.IsNullOrWhiteSpace(model.Movil) ? null : model.Movil.Trim();
        model.Email = (model.Email ?? string.Empty).Trim();
        model.Direc = string.IsNullOrWhiteSpace(model.Direc) ? null : model.Direc.Trim();
        model.DeptName = (model.DeptName ?? string.Empty).Trim();
    }
}
