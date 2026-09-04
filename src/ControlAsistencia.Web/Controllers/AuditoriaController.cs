using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class AuditoriaController : Controller
{
    private const int PageSize = 30;
    private readonly IAuditoriaRepository _repository;

    public AuditoriaController(IAuditoriaRepository repository)
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

            // Se registra la consulta después de obtener el resultado para que
            // el propio evento de auditoría no altere la página que se está mostrando.
            await _repository.RegisterViewAuditAsync(GetOperatorName(), GetMachineAlias());

            return View(new AuditoriaIndexViewModel
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
            TempData["ErrorMessage"] = "No fue posible cargar el listado de auditoría.";
            return View(new AuditoriaIndexViewModel
            {
                PageNumber = currentPage,
                PageSize = PageSize,
                Search = search
            });
        }
    }

    private async Task<bool> EnsurePermissionAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) ||
            !await _repository.CanViewAuditAsync(userId))
        {
            TempData["ErrorMessage"] = "No tiene permisos para acceder a Auditoría.";
            return false;
        }

        return true;
    }

    private string GetOperatorName() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-";

    private string GetMachineAlias() => Environment.MachineName;
}
