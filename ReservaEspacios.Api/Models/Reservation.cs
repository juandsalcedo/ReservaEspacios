namespace ReservaEspacios.Api.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SpaceId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Activa;
    public DateTime FechaCreacion { get; set; }

    public User User { get; set; } = null!;
    public Space Space { get; set; } = null!;
}
