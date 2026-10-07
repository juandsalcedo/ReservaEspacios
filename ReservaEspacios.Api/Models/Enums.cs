namespace ReservaEspacios.Api.Models;

public enum RolUsuario
{
    Estudiante,
    Docente,
    Admin
}

public enum TipoEspacio
{
    Aula,
    Laboratorio,
    SalaEstudio,
    Ludico,
    Auditorio
}

public enum EstadoReserva
{
    Activa,
    Cancelada,
    Completada
}

// Los valores coinciden con System.DayOfWeek para poder convertir la fecha de la reserva.
public enum DiaSemana
{
    Domingo = 0,
    Lunes = 1,
    Martes = 2,
    Miercoles = 3,
    Jueves = 4,
    Viernes = 5,
    Sabado = 6
}
