using System.Text.Json.Serialization;

namespace ReservaEspacios.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Facultad { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    [JsonIgnore]
    public string? PasswordResetTokenHash { get; set; }

    [JsonIgnore]
    public DateTime? PasswordResetTokenExpires { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
