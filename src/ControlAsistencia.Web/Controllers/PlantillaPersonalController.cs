using ControlAsistencia.Web.Models;
using ControlAsistencia.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlAsistencia.Web.Controllers;

[Authorize]
public class PlantillaPersonalController : Controller
{
    private readonly IPlantillaPersonalRepository _repository;

    public PlantillaPersonalController(IPlantillaPersonalRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var departments = await _repository.GetDepartmentsHierarchyAsync();

            return View(new PlantillaPersonalIndexViewModel
            {
                Departments = departments
            });
        }
        catch
        {
            TempData["ErrorMessage"] = "No fue posible cargar la pantalla.";
            return View(new PlantillaPersonalIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> EmpleadosPorDepartamento(
        int deptId,
        bool includeSubDependencies = false)
    {
        try
        {
            if (!await _repository.DepartmentExistsAsync(deptId))
            {
                return NotFound(new
                {
                    success = false,
                    message = "El departamento seleccionado no existe."
                });
            }

            var items = await _repository.GetEmployeesByDepartmentAsync(
                deptId,
                includeSubDependencies);

            var data = items.Select(x => new
            {
                userId = x.UserId,
                badgeNumber = x.BadgeNumber,
                ssn = x.Ssn,
                name = x.Name,
                defaultDeptId = x.DefaultDeptId,
                departmentName = x.DepartmentName,
                photoBase64 = x.PhotoBase64,
                privilege = x.Privilege,
                privilegeDescription = x.PrivilegeDescription
            });

            return Json(new { success = true, items = data });
        }
        catch
        {
            return BadRequest(new
            {
                success = false,
                message = "No fue posible cargar las personas del área seleccionada."
            });
        }
    }
}
