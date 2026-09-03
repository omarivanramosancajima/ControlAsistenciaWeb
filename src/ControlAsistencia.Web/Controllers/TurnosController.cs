using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class TurnosController : Controller
{
    private const int PageSize = 10;
    private readonly ITurnoRepository _repository;

    public TurnosController(ITurnoRepository repository) => _repository = repository;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var currentPage = page <= 0 ? 1 : page;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        try
        {
            var result = await _repository.GetPagedAsync(currentPage, PageSize, search);
            await _repository.RegisterViewAuditAsync("Listado", GetOperatorName(), GetMachineAlias());
            return View(new TurnoIndexViewModel { Turnos = result.Items, PageNumber = currentPage, PageSize = PageSize, TotalRecords = result.TotalRecords, Search = search });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"No fue posible cargar el listado de turnos ({ex.Message})";
            return View(new TurnoIndexViewModel { PageNumber = 1, PageSize = PageSize });
        }
    }

    [HttpGet] public IActionResult Create() => View(BuildForm(new TurnoFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TurnoFormViewModel model)
    {
        await ValidateUniqueAsync(model.NAME, null);
        if (!ModelState.IsValid) return View(BuildForm(model));
        var result = await _repository.CreateAsync(model, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : View(BuildForm(model));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var turno = await _repository.GetByIdAsync(id);
        if (turno is null) { TempData["ErrorMessage"] = "No se encontró el turno solicitado."; return RedirectToAction(nameof(Index)); }
        return View(BuildForm(new TurnoFormViewModel { NUM_RUNID = turno.NUM_RUNID, NAME = turno.NAME, STARTDATE = turno.STARTDATE, ENDDATE = turno.ENDDATE, CYLE = turno.CYLE ?? 1, UNITS = turno.UNITS ?? 1 }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TurnoFormViewModel model)
    {
        await ValidateUniqueAsync(model.NAME, model.NUM_RUNID);
        if (!ModelState.IsValid) return View(BuildForm(model));
        var result = await _repository.UpdateAsync(model, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : View(BuildForm(model));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var turno = await _repository.GetByIdAsync(id);
        if (turno is null) { TempData["ErrorMessage"] = "No se encontró el turno solicitado."; return RedirectToAction(nameof(Index)); }
        return View(new DeleteTurnoViewModel { Turno = turno });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int numRunid)
    {
        var dependency = await _repository.ValidateDeleteAsync(numRunid);
        if (dependency.HasDependency) { TempData["ErrorMessage"] = dependency.DependencyMessage; return RedirectToAction(nameof(Delete), new { id = numRunid }); }
        var result = await _repository.DeleteAsync(numRunid, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private static TurnoFormViewModel BuildForm(TurnoFormViewModel model)
    {
        if (model.UNITS is null) model.UNITS = 1;
        if (model.CYLE is null) model.CYLE = 1;
        return model;
    }

    private async Task ValidateUniqueAsync(string? name, int? excludeId)
    {
        if (!string.IsNullOrWhiteSpace(name) && await _repository.ExistsByNameAsync(name.Trim(), excludeId))
        {
            ModelState.AddModelError(nameof(TurnoFormViewModel.NAME), "El nombre ya se encuentra registrado.");
        }
    }

    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";
    private string GetMachineAlias() => Environment.MachineName;
}