using ReservaEspacios.Api.DTOs;

namespace ReservaEspacios.Api.Services;

public interface IUserService
{
    Task<UserProfileResponse> GetProfile(int userId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateProfile(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task ChangePassword(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfileResponse>> ListUsers(int adminId, CancellationToken cancellationToken = default);
    Task<UserProfileResponse> UpdateRole(int adminId, int userId, string? rol, CancellationToken cancellationToken = default);
}
