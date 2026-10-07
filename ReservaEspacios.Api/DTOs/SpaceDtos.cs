using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.DTOs;

public class CreateSpaceRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string HorarioFuncionamiento { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
}

public class UpdateSpaceRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string HorarioFuncionamiento { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
}

public class SpaceResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string HorarioFuncionamiento { get; set; } = string.Empty;
    public bool Disponible { get; set; }
    public IReadOnlyList<SpaceHorarioResponse> Horarios { get; set; } = Array.Empty<SpaceHorarioResponse>();
}
