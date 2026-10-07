namespace ReservaEspacios.Api.Models;

public class SpaceHorario
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool EstaDisponible { get; set; } = true;

    public Space Space { get; set; } = null!;
}
