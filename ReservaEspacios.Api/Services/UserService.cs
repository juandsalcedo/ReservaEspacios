using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;

namespace ReservaEspacios.Api.Services;

public class UserService : IUserService
{
    private readonly ReservaEspaciosContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ReservaEspaciosContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProfileResponse> GetProfile(int userId, CancellationToken cancellationToken = default)
    {
        var usuario = await ObtenerUsuario(userId, cancellationToken);
        return AuthService.MapProfile(usuario);
    }

    public async Task<UserProfileResponse> UpdateProfile(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var usuario = await ObtenerUsuario(userId, cancellationToken);

        var nombre = request.Nombre?.Trim() ?? string.Empty;
        var telefono = request.Telefono?.Trim() ?? string.Empty;
        var facultad = request.Facultad?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ApiException("El nombre es obligatorio.", StatusCodes.Status400BadRequest);
        if (nombre.Length > 120)
            throw new ApiException("El nombre no puede superar 120 caracteres.", StatusCodes.Status400BadRequest);
        if (telefono.Length > 30)
            throw new ApiException("El teléfono no puede superar 30 caracteres.", StatusCodes.Status400BadRequest);
        if (facultad.Length > 150)
            throw new ApiException("La facultad no puede superar 150 caracteres.", StatusCodes.Status400BadRequest);

        usuario.Nombre = nombre;
        usuario.Telefono = telefono;
        usuario.Facultad = facultad;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Perfil del usuario {UserId} actualizado.", userId);
        return AuthService.MapProfile(usuario);
    }

    public async Task ChangePassword(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var usuario = await ObtenerUsuario(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.PasswordActual) || string.IsNullOrWhiteSpace(request.PasswordNueva))
            throw new ApiException("La contraseña actual y la nueva son obligatorias.", StatusCodes.Status400BadRequest);
        if (request.PasswordNueva.Length < 6)
            throw new ApiException("La contraseña debe tener al menos 6 caracteres.", StatusCodes.Status400BadRequest);
        if (request.PasswordNueva.Length > 72)
            throw new ApiException("La contraseña no puede superar 72 caracteres.", StatusCodes.Status400BadRequest);
        if (!AuthService.PasswordCoincide(request.PasswordActual, usuario.PasswordHash))
            throw new ApiException("La contraseña actual no es correcta.", StatusCodes.Status400BadRequest);
        if (request.PasswordActual == request.PasswordNueva)
            throw new ApiException("La nueva contraseña debe ser distinta a la actual.", StatusCodes.Status400BadRequest);

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("El usuario {UserId} cambió su contraseña.", userId);
    }

    private async Task<Models.User> ObtenerUsuario(int userId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (usuario is null)
            throw new ApiException("El usuario autenticado no existe.", StatusCodes.Status401Unauthorized);

        return usuario;
    }
}
