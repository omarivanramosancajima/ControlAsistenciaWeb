using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class EmpresaItemViewModel
{
    public int CompanyId { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string Descrip { get; set; } = string.Empty;
    public string? Tel { get; set; }
    public string? Movil { get; set; }
    public string? Email { get; set; }
    public string? DeptName { get; set; }
    public int DeptId { get; set; }
}

public class EmpresaFormViewModel
{
    public int CompanyId { get; set; }

    [Display(Name = "RUC")]
    [Required(ErrorMessage = "El RUC es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener exactamente 11 dígitos numéricos.")]
    [StringLength(15)]
    public string TaxId { get; set; } = string.Empty;

    [Display(Name = "Razón social")]
    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(100)]
    public string Descrip { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [StringLength(15, ErrorMessage = "El teléfono no puede superar 15 caracteres.")]
    public string? Tel { get; set; }

    [Display(Name = "Móvil")]
    [StringLength(15, ErrorMessage = "El móvil no puede superar 15 caracteres.")]
    public string? Movil { get; set; }

    [Display(Name = "Email")]
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
    [StringLength(30, ErrorMessage = "El email no puede superar 30 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Dirección")]
    [StringLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Direc { get; set; }

    [Display(Name = "Abreviatura")]
    [Required(ErrorMessage = "La abreviatura es obligatoria.")]
    [StringLength(30, ErrorMessage = "La abreviatura no puede superar 30 caracteres.")]
    public string DeptName { get; set; } = string.Empty;
}

public class EmpresaDeleteViewModel
{
    public int CompanyId { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string Descrip { get; set; } = string.Empty;
    public string? Tel { get; set; }
    public string? Movil { get; set; }
    public string? Email { get; set; }
    public string? Direc { get; set; }
    public string? DeptName { get; set; }
    public int DeptId { get; set; }
}
