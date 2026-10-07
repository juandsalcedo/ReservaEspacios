using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public interface ICurrentUserService
{
    int GetUserId();
    RolUsuario GetRole();
}
