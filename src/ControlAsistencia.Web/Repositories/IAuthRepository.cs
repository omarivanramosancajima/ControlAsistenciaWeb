using ControlAsistencia.Web.Models;

namespace ControlAsistencia.Web.Repositories;

public interface IAuthRepository
{
    Task<UsuarioLoginDTO?> ValidarLoginAsync(string badgeNumber, string password);
}