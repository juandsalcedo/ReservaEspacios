using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.DTOs;

public class AuthRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Telefono { get; set; }
    public string? Facultad { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public int UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
}
