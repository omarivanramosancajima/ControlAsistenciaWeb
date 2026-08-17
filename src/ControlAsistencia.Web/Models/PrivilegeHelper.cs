namespace ControlAsistencia.Web.Models;

public static class PrivilegeHelper
{
    public static string GetDescription(int? privilege) => privilege switch
    {
        -1 => "Inválido",
        0 => "Usuario",
        1 => "Enrolar",
        2 => "Administrador",
        3 => "Supervisor",
        _ => "Desconocido"
    };
}