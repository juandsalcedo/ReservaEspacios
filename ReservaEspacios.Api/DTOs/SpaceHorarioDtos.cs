using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.DTOs;

public class CreateSpaceHorarioRequest
{
    public int SpaceId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool EstaDisponible { get; set; } = true;
}

public class UpdateSpaceHorarioRequest
{
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool EstaDisponible { get; set; } = true;
}

public class SpaceHorarioResponse
{
    public int Id { get; set; }
    public int SpaceId { get; set; }
    public string? NombreEspacio { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool EstaDisponible { get; set; }
}
