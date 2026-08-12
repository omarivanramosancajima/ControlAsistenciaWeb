using System.Security.Claims;
using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthRepository _authRepository;

    public AuthController(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new UsuarioLoginDTO());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UsuarioLoginDTO model)
    {
        if (string.IsNullOrWhiteSpace(model.BadgeNumber))
        {
            ModelState.AddModelError(nameof(model.BadgeNumber), "El código de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var usuario = await _authRepository.ValidarLoginAsync(model.BadgeNumber.Trim(), model.Password);

            if (usuario is null)
            {
                ViewBag.ErrorMessage = "Credenciales inválidas. Verifica tu usuario y contraseña.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.UserId.ToString()),
                new(ClaimTypes.Name, string.IsNullOrWhiteSpace(usuario.Name) ? usuario.BadgeNumber : usuario.Name),
                new("BadgeNumber", usuario.BadgeNumber),
                new("SecurityFlags", usuario.SecurityFlags?.ToString() ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return RedirectToAction("Index", "Home");
        }
        catch (Exception)
        {
            ViewBag.ErrorMessage = "No fue posible iniciar sesión en este momento. Intenta nuevamente.";
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}