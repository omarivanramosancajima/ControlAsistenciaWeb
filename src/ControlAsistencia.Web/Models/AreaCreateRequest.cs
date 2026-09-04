using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class AreaCreateRequest
{
    [Required(ErrorMessage = "Debe seleccionar un área padre.")]
    public int? ParentDeptId { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre del área.")]
    [StringLength(30, ErrorMessage = "El nombre del área no puede superar los 30 caracteres.")]
    public string? DeptName { get; set; }
}
