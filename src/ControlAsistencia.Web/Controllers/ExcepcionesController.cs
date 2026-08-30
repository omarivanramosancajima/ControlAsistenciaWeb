using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class ExcepcionesController : Controller
{
    private const int PageSize = 10;
    private readonly IExcepcionRepository _repository;
    private readonly ILogger<ExcepcionesController> _logger;

    public ExcepcionesController(IExcepcionRepository repository, ILogger<ExcepcionesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var (items, totalRecords) = await _repository.GetPagedAsync(page, PageSize);
        foreach (var item in items) await _repository.RegisterViewAuditAsync(item.LeaveName, GetOperatorName(), GetMachineAlias());
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(BuildModel(new ExcepcionDTO()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExcepcionDTO model)
    {
        if (!ModelState.IsValid) return View(BuildModel(model));
        if (await _repository.ExistsByNameAsync(model.LeaveName))
        {
            ModelState.AddModelError(nameof(model.LeaveName), "Ya existe una excepción con ese nombre.");
            return View(BuildModel(model));
        }
        var result = await _repository.CreateAsync(model, GetOperatorName(), GetMachineAlias());
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "No fue posible registrar la excepción."); return View(BuildModel(model)); }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? NotFound() : View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ExcepcionDTO model)
    {
        if (id != model.LeaveId) return NotFound();
        if (!ModelState.IsValid) return View(BuildModel(model));
        if (await _repository.ExistsByNameAsync(model.LeaveName, model.LeaveId))
        {
            ModelState.AddModelError(nameof(model.LeaveName), "Ya existe una excepción con ese nombre.");
            return View(BuildModel(model));
        }
        var result = await _repository.UpdateAsync(model, GetOperatorName(), GetMachineAlias());
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message ?? "No fue posible actualizar la excepción."); return View(BuildModel(model)); }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return NotFound();
        ViewBag.DeleteMessage = TempData["ErrorMessage"] as string;
        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int leaveId)
    {
        var result = await _repository.DeleteAsync(leaveId, GetOperatorName(), GetMachineAlias());
        if (!result.Success) { TempData["ErrorMessage"] = result.Message; return RedirectToAction(nameof(Delete), new { id = leaveId }); }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private static ExcepcionDTO BuildModel(ExcepcionDTO model)
    {
        model.MinUnit = model.MinUnit <= 0 ? 1.00m : model.MinUnit;
        model.Unit = model.Unit is < 1 or > 3 ? 1 : model.Unit;
        return model;
    }

    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";
    private string GetMachineAlias() => Environment.MachineName;
}