using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class FeriadosController : Controller
{
    private readonly IFeriadoRepository _repository;
    private readonly ILogger<FeriadosController> _logger;

    public FeriadosController(
        IFeriadoRepository repository,
        ILogger<FeriadosController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        try
        {
            const int pageSize = 10;
            var currentPage = page <= 0 ? 1 : page;
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var (items, totalRecords) = await _repository.GetPagedAsync(currentPage, pageSize, search);

            // Registrar auditoría para cada feriado en la página
            var operatorName = User.Identity?.Name ?? "Unknown";
            var machineAlias = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            foreach (var item in items)
            {
                await _repository.RegisterViewAuditAsync(item.HolidayName, operatorName, machineAlias);
            }

            ViewBag.CurrentPage = currentPage;
            ViewBag.Search = search;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            return View(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener feriados");
            return View("Error", new ErrorViewModel { Message = "Error al cargar la lista de feriados" });
        }
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeriadoDTO model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var operatorName = User.Identity?.Name ?? "Unknown";
            var machineAlias = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _repository.CreateAsync(model, operatorName, machineAlias);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear feriado");
            ModelState.AddModelError("", "Error al crear feriado");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var feriado = await _repository.GetByIdAsync(id);
            if (feriado == null)
            {
                return NotFound();
            }
            return View(feriado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar feriado para edición");
            return View("Error", new ErrorViewModel { Message = "Error al cargar feriado" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeriadoDTO model)
    {
        if (id != model.HolidayId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var operatorName = User.Identity?.Name ?? "Unknown";
            var machineAlias = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _repository.UpdateAsync(model, operatorName, machineAlias);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar feriado");
            ModelState.AddModelError("", "Error al actualizar feriado");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var feriado = await _repository.GetByIdAsync(id);
            if (feriado == null)
            {
                return NotFound();
            }
            return View(feriado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar feriado para eliminación");
            return View("Error", new ErrorViewModel { Message = "Error al cargar feriado" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var operatorName = User.Identity?.Name ?? "Unknown";
            var machineAlias = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _repository.DeleteAsync(id, operatorName, machineAlias);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar feriado");
            TempData["ErrorMessage"] = "Error al eliminar feriado";
            return RedirectToAction(nameof(Delete), new { id });
        }
    }
}