using System.Security.Claims;
using ReservaEspacios.Api.Exceptions;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetUserId()
    {
        var userId = ReadClaim("sub") ?? ReadClaim(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var id) || id <= 0)
            throw new ApiException("No se pudo identificar al usuario autenticado.", StatusCodes.Status401Unauthorized);

        return id;
    }

    public RolUsuario GetRole()
    {
        var role = ReadClaim("role") ?? ReadClaim(ClaimTypes.Role);
        if (!Enum.TryParse<RolUsuario>(role, out var rol))
            throw new ApiException("No se pudo identificar el rol del usuario autenticado.", StatusCodes.Status401Unauthorized);

        return rol;
    }

    private string? ReadClaim(string claimType)
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
    }
}
