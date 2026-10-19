namespace ReservaEspacios.Api.DTOs;

public class DashboardStatsResponse
{
    public int TotalReservas { get; set; }
    public int ReservasActivas { get; set; }
    public int ReservasCanceladas { get; set; }
    public string EspacioMasPopular { get; set; } = string.Empty;
    public IEnumerable<EspacioStatDto> ReservasPorEspacio { get; set; } = [];
    public IEnumerable<EstadoStatDto> ReservasPorEstado { get; set; } = [];

    public class EspacioStatDto
    {
        public string NombreEspacio { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class EstadoStatDto
    {
        public string NombreEstado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
