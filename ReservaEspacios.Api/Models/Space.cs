namespace ReservaEspacios.Api.Models;

public class Space
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public TipoEspacio Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string HorarioFuncionamiento { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<SpaceHorario> Horarios { get; set; } = new List<SpaceHorario>();
}
