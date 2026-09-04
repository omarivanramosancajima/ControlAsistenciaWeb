using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class AreasController : Controller
{
    private readonly IAreaRepository _repository;
    private readonly ILogger<AreasController> _logger;

    public AreasController(
        IAreaRepository repository,
        ILogger<AreasController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var areas = await _repository.GetHierarchyAsync();
            return View(new AreaIndexViewModel { Areas = areas });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar áreas.");
            return View("Error", new ErrorViewModel
            {
                Message = "Error al cargar la lista de áreas."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AreaCreateRequest model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Debe seleccionar un área padre e ingresar un nombre válido." });

        try
        {
            var result = await _repository.CreateAsync(model.ParentDeptId!.Value, model.DeptName!);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear área.");
            return Json(new { success = false, message = "No fue posible crear el área." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AreaEditRequest model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Debe ingresar un nombre válido." });

        try
        {
            var result = await _repository.UpdateAsync(model.DeptId, model.DeptName!);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar área.");
            return Json(new { success = false, message = "No fue posible actualizar el área." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
            return Json(new { success = false, message = "Debe seleccionar un área válida." });

        try
        {
            var result = await _repository.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar área.");
            return Json(new { success = false, message = "No fue posible eliminar el área." });
        }
    }
}
