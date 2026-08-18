using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class HorariosController : Controller
{
    private const int PageSize = 10;
    private readonly IHorarioRepository _repository;

    public HorariosController(IHorarioRepository repository) 
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var currentPage = page <= 0 ? 1 : page;
        string x = "1.0";
        try
        {
            x = "1.1";
            var result = await _repository.GetPagedAsync(currentPage, PageSize);
            x = "1.2";
            if (result.Items.Count > 0) await _repository.RegisterViewAuditAsync(result.Items[0].SchName, GetOperatorName(), GetMachineAlias());
            x = "1.3";
            return View(new HorarioIndexViewModel { Horarios = result.Items, PageNumber = currentPage, PageSize = PageSize, TotalRecords = result.TotalRecords });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "No fue posible cargar el listado de horarios ("+x+"-"+ex.Message+")";
            return View(new HorarioIndexViewModel { PageNumber = 1, PageSize = PageSize });
        }
    }

    [HttpGet] public IActionResult Create() => View(BuildForm(new HorarioFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HorarioFormViewModel model)
    {
        await ValidateUniqueAsync(model.SchName, null);
        if (!ModelState.IsValid) return View(BuildForm(model));
        var result = await _repository.CreateAsync(model, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : View(BuildForm(model));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var horario = await _repository.GetByIdAsync(id);
        if (horario is null) { TempData["ErrorMessage"] = "No se encontró el horario solicitado."; return RedirectToAction(nameof(Index)); }
        return View(BuildForm(new HorarioFormViewModel { SchClassid = horario.SchClassid, SchName = horario.SchName, StartTime = horario.StartTime?.ToString(@"HH\:mm") ?? string.Empty, EndTime = horario.EndTime?.ToString(@"HH\:mm") ?? string.Empty, LateMinutes = horario.LateMinutes, EarlyMinutes = horario.EarlyMinutes, Color = horario.Color ?? 16715535, CheckIn = (horario.CHECKIN ?? 1) == 1, CheckOut = (horario.CHECKOUT ?? 1) == 1 }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HorarioFormViewModel model)
    {
        await ValidateUniqueAsync(model.SchName, model.SchClassid);
        if (!ModelState.IsValid) return View(BuildForm(model));
        var result = await _repository.UpdateAsync(model, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return result.Success ? RedirectToAction(nameof(Index)) : View(BuildForm(model));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var horario = await _repository.GetByIdAsync(id);
        if (horario is null) { TempData["ErrorMessage"] = "No se encontró el horario solicitado."; return RedirectToAction(nameof(Index)); }
        return View(new DeleteHorarioViewModel { Horario = horario });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int schClassid)
    {
        var dependency = await _repository.ValidateDeleteAsync(schClassid);
        if (dependency.HasDependency) { TempData["ErrorMessage"] = dependency.DependencyMessage; return RedirectToAction(nameof(Delete), new { id = schClassid }); }
        var result = await _repository.DeleteAsync(schClassid, GetOperatorName(), GetMachineAlias());
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private HorarioFormViewModel BuildForm(HorarioFormViewModel model)
    {
        model.Color = model.Color == 0 ? 16715535 : model.Color;
        if (!model.CheckIn) model.CheckIn = false;
        if (!model.CheckOut) model.CheckOut = false;
        return model;
    }

    private async Task ValidateUniqueAsync(string? name, int? excludeId)
    {
        if (!string.IsNullOrWhiteSpace(name) && await _repository.ExistsByNameAsync(name.Trim(), excludeId))
        {
            ModelState.AddModelError(nameof(HorarioFormViewModel.SchName), "El nombre ya se encuentra registrado.");
        }
    }
    private string GetOperatorName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";
    private string GetMachineAlias() => Environment.MachineName;
}