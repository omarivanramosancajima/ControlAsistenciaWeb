using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class CambioDeClaveController : Controller
{
    private readonly ICambioDeClaveRepository _repository;

    public CambioDeClaveController(ICambioDeClaveRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        try
        {
            var user = await _repository.GetAuthenticatedUserAsync(userId);
            if (user is null)
            {
                TempData["ErrorMessage"] = "No fue posible obtener los datos del usuario autenticado.";
                return RedirectToAction("Index", "Home");
            }

            return View(new CambioDeClaveViewModel
            {
                BadgeNumber = user.BadgeNumber,
                Name = user.Name
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la opción Cambio de clave.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CambioDeClaveViewModel model)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            await RestoreAuthenticatedUserDataAsync(model, userId);
            return View(model);
        }

        try
        {
            var result = await _repository.ChangePasswordAsync(
                userId,
                model.CurrentPassword!,
                model.NewPassword!,
                User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "-",
                Environment.MachineName);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await RestoreAuthenticatedUserDataAsync(model, userId);
                return View(model);
            }

            model.CurrentPassword = string.Empty;
            model.NewPassword = string.Empty;
            model.ConfirmNewPassword = string.Empty;
            model.SuccessMessage = "El cambio de clave se realizó correctamente.";

            return View(model);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "No fue posible guardar el cambio de clave.");
            await RestoreAuthenticatedUserDataAsync(model, userId);
            return View(model);
        }
    }

    private async Task RestoreAuthenticatedUserDataAsync(CambioDeClaveViewModel model, int userId)
    {
        var user = await _repository.GetAuthenticatedUserAsync(userId);
        if (user is not null)
        {
            model.BadgeNumber = user.BadgeNumber;
            model.Name = user.Name;
        }
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
