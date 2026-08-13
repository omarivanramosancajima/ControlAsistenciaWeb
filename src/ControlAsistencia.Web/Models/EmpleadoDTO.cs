using System.ComponentModel.DataAnnotations;

namespace ControlAsistencia.Web.Models;

public class EmpleadoDTO
{
    public int UserId { get; set; }

    public int USERID
    {
        get => UserId;
        set => UserId = value;
    }

    [Display(Name = "Código")]
    public string BadgeNumber { get; set; } = string.Empty;

    public string BADGENUMBER
    {
        get => BadgeNumber;
        set => BadgeNumber = value;
    }

    [Display(Name = "DNI")]
    public string? Ssn { get; set; }

    public string? SSN
    {
        get => Ssn;
        set => Ssn = value;
    }

    [Display(Name = "Nombre")]
    public string? Name { get; set; }

    public string? NAME
    {
        get => Name;
        set => Name = value;
    }

    [Display(Name = "Género")]
    public string? Gender { get; set; }

    public string? GENDER
    {
        get => Gender;
        set => Gender = value;
    }

    [Display(Name = "Sueldo")]
    public string? Title { get; set; }

    public string? TITLE
    {
        get => Title;
        set => Title = value;
    }

    [Display(Name = "No. Móvil")]
    public string? Pager { get; set; }

    public string? PAGER
    {
        get => Pager;
        set => Pager = value;
    }

    [Display(Name = "Fecha Nac.")]
    [DataType(DataType.Date)]
    public DateTime? Birthday { get; set; }

    public DateTime? BIRTHDAY
    {
        get => Birthday;
        set => Birthday = value;
    }

    [Display(Name = "Fecha Contrato")]
    [DataType(DataType.Date)]
    public DateTime? HiredDay { get; set; }

    public DateTime? HIREDDAY
    {
        get => HiredDay;
        set => HiredDay = value;
    }

    [Display(Name = "Dirección")]
    public string? Street { get; set; }

    public string? STREET
    {
        get => Street;
        set => Street = value;
    }

    [Display(Name = "Telf. Oficina")]
    public string? OPhone { get; set; }

    public string? OPHONE
    {
        get => OPhone;
        set => OPhone = value;
    }

    [Display(Name = "Área/Ubic.")]
    public int? DefaultDeptId { get; set; }

    public int? DEFAULTDEPTID
    {
        get => DefaultDeptId;
        set => DefaultDeptId = value;
    }

    public string? DepartmentName { get; set; }

    [Display(Name = "Nacionalidad")]
    public string? Minzu { get; set; }

    public string? MINZU
    {
        get => Minzu;
        set => Minzu = value;
    }

    [Display(Name = "Clave Equipo")]
    public string? MVerifyPass { get; set; }

    public string? MVERIFYPASS
    {
        get => MVerifyPass;
        set => MVerifyPass = value;
    }

    [Display(Name = "Fotografía")]
    public byte[]? Photo { get; set; }

    public string? PhotoBase64 { get; set; }

    [Display(Name = "Perfil Equipo")]
    public int? Privilege { get; set; }

    public int? PRIVILEGE
    {
        get => Privilege;
        set => Privilege = value;
    }

    public string? PrivilegeDescription { get; set; }

    [Display(Name = "No. Tarjeta")]
    public string? CardNo { get; set; }

    public string? CARDNO
    {
        get => CardNo;
        set => CardNo = value;
    }
}