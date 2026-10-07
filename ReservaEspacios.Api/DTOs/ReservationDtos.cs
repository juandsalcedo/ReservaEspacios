using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.DTOs;

public class CreateReservationRequest
{
    public int SpaceId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class UpdateReservationRequest
{
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class ReservationResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int SpaceId { get; set; }
    public string NombreEspacio { get; set; } = string.Empty;
    public string UbicacionEspacio { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public EstadoReserva Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public IReadOnlyList<string>? Detalles { get; set; }
}
