using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.DTOs;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Facultad { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}

public class UpdateProfileRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Facultad { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
}

public class MessageResponse
{
    public string Mensaje { get; set; } = string.Empty;
}
