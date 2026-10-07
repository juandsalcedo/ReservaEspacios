using ReservaEspacios.Api.DTOs;

namespace ReservaEspacios.Api.Services;

public interface IAuthService
{
    Task<UserProfileResponse> Register(AuthRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> Login(AuthRequest request, CancellationToken cancellationToken = default);
}
