namespace ReservaEspacios.Api.Services;

public interface IEmailService
{
    Task SendReservationConfirmationAsync(
        string emailDestino,
        string nombreUsuario,
        string nombreEspacio,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin);

    Task<bool> SendPasswordResetAsync(string emailDestino, string nombreUsuario, string enlace);
}
