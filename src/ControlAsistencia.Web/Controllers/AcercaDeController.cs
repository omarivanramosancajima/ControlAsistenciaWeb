using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class AcercaDeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
