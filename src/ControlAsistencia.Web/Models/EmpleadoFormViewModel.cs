using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ControlAsistencia.Web.Models;

public class EmpleadoFormViewModel : IValidatableObject
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "El campo Código es obligatorio.")]
    [Display(Name = "Código")]
    public string BadgeNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo DNI es obligatorio.")]
    [Display(Name = "DNI")]
    public string Ssn { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo Género es obligatorio.")]
    [Display(Name = "Género")]
    public string Gender { get; set; } = string.Empty;

    [Display(Name = "Sueldo")]
    public string? Title { get; set; }

    [Display(Name = "No. Móvil")]
    public string? Pager { get; set; }

    [Display(Name = "Fecha Nac.")]
    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    [Display(Name = "Fecha Contrato")]
    [DataType(DataType.Date)]
    public DateTime? HiredDay { get; set; }

    [Display(Name = "Dirección")]
    public string? Street { get; set; }

    [Display(Name = "Telf. Oficina")]
    public string? OPhone { get; set; }

    [Required(ErrorMessage = "El campo Área/Ubic. es obligatorio.")]
    [Display(Name = "Área/Ubic.")]
    public int? DefaultDeptId { get; set; }

    public string? DepartmentName { get; set; }

    [Display(Name = "Nacionalidad")]
    public string? Minzu { get; set; }

    [Display(Name = "Clave Equipo")]
    public string? MVerifyPass { get; set; }

    public byte[]? CurrentPhoto { get; set; }
    public string? CurrentPhotoBase64 { get; set; }

    [Display(Name = "Fotografía")]
    public IFormFile? PhotoFile { get; set; }

    [Required(ErrorMessage = "El campo Perfil Equipo es obligatorio.")]
    [Display(Name = "Perfil Equipo")]
    public int? Privilege { get; set; }

    [Display(Name = "No. Tarjeta")]
    public string? CardNo { get; set; }

    public IReadOnlyList<SelectListItem> GenderOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> PrivilegeOptions { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Gender) && Gender is not ("M" or "F"))
        {
            yield return new ValidationResult("El género seleccionado no es válido.", new[] { nameof(Gender) });
        }

        var privileges = new[] { -1, 0, 1, 2, 3 };
        if (Privilege.HasValue && !privileges.Contains(Privilege.Value))
        {
            yield return new ValidationResult("El perfil de equipo seleccionado no es válido.", new[] { nameof(Privilege) });
        }
    }
}