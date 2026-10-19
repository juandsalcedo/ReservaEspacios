using System.Data;
using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class ReservationService : IReservationService
{
    private readonly ReservaEspaciosContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        ReservaEspaciosContext context,
        IEmailService emailService,
        ILogger<ReservationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetUserReservations(int userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExists(userId, cancellationToken);

        var reservas = await _context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.User)
            .Include(reservation => reservation.Space)
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.Fecha)
            .ThenBy(reservation => reservation.HoraInicio)
            .ToListAsync(cancellationToken);

        return reservas.Select(Map).ToList();
    }

    public async Task<ReservationResponse> GetReservation(int id, int userId, CancellationToken cancellationToken = default)
    {
        var reserva = await _context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.User)
            .Include(reservation => reservation.Space)
            .FirstOrDefaultAsync(reservation => reservation.Id == id && reservation.UserId == userId, cancellationToken);

        if (reserva is null)
            throw new ApiException("No se encontró la reserva.", StatusCodes.Status404NotFound);

        return Map(reserva);
    }

    public async Task<ReservationResponse> CreateReservation(int userId, CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarRango(request.Fecha, request.HoraInicio, request.HoraFin);
        if (request.SpaceId <= 0)
            throw new ApiException("El espacio es obligatorio.", StatusCodes.Status400BadRequest);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            throw new ApiException("El usuario autenticado no existe.", StatusCodes.Status401Unauthorized);

        await ValidateAvailability(request.SpaceId, request.Fecha, request.HoraInicio, request.HoraFin, cancellationToken: cancellationToken);

        var reserva = new Reservation
        {
            UserId = userId,
            SpaceId = request.SpaceId,
            Fecha = request.Fecha,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Estado = EstadoReserva.Activa,
            FechaCreacion = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            User = user
        };

        _context.Reservations.Add(reserva);
        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(reserva).Reference(r => r.Space).LoadAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await TrySendConfirmationAsync(user, reserva);

        _logger.LogInformation(
            "Reserva {ReservaId} creada por el usuario {UserId} en el espacio {SpaceId} para {Fecha} de {Inicio} a {Fin}.",
            reserva.Id, userId, reserva.SpaceId, reserva.Fecha, reserva.HoraInicio, reserva.HoraFin);

        return Map(reserva);
    }

    public async Task<ReservationResponse> UpdateReservation(int id, int userId, UpdateReservationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidarRango(request.Fecha, request.HoraInicio, request.HoraFin);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var reserva = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

        if (reserva is null)
            throw new ApiException("No se encontró la reserva.", StatusCodes.Status404NotFound);

        if (reserva.Estado != EstadoReserva.Activa)
            throw new ApiException("Solo se pueden modificar reservas activas.", StatusCodes.Status400BadRequest);

        await ValidateAvailability(reserva.SpaceId, request.Fecha, request.HoraInicio, request.HoraFin, reserva.Id, cancellationToken);

        reserva.Fecha = request.Fecha;
        reserva.HoraInicio = request.HoraInicio;
        reserva.HoraFin = request.HoraFin;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Reserva {ReservaId} actualizada por el usuario {UserId} para {Fecha} de {Inicio} a {Fin}.",
            reserva.Id, userId, reserva.Fecha, reserva.HoraInicio, reserva.HoraFin);

        return Map(reserva);
    }

    public async Task<ReservationResponse> CancelReservation(int id, int userId, CancellationToken cancellationToken = default)
    {
        var reserva = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Space)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

        if (reserva is null)
            throw new ApiException("No se encontró la reserva.", StatusCodes.Status404NotFound);

        if (reserva.Estado == EstadoReserva.Cancelada)
            throw new ApiException("La reserva ya está cancelada.", StatusCodes.Status400BadRequest);

        if (reserva.Estado != EstadoReserva.Activa)
            throw new ApiException("Solo se pueden cancelar reservas activas.", StatusCodes.Status400BadRequest);

        reserva.Estado = EstadoReserva.Cancelada;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reserva {ReservaId} cancelada por el usuario {UserId}.", reserva.Id, userId);

        return Map(reserva);
    }

    public async Task<DashboardStatsResponse> GetDashboardStatisticsAsync(CancellationToken ct)
    {
        var reservas = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Space)
            .ToListAsync(ct);

        var porEspacio = reservas
            .GroupBy(reserva =>
            {
                var nombre = reserva.Space?.Nombre;
                return string.IsNullOrWhiteSpace(nombre) ? "Sin espacio" : nombre;
            })
            .Select(grupo => new DashboardStatsResponse.EspacioStatDto
            {
                NombreEspacio = grupo.Key,
                Cantidad = grupo.Count()
            })
            .OrderByDescending(item => item.Cantidad)
            .ThenBy(item => item.NombreEspacio)
            .ToList();

        var porEstado = reservas
            .GroupBy(reserva => reserva.Estado)
            .Select(grupo => new DashboardStatsResponse.EstadoStatDto
            {
                NombreEstado = grupo.Key.ToString(),
                Cantidad = grupo.Count()
            })
            .OrderByDescending(item => item.Cantidad)
            .ThenBy(item => item.NombreEstado)
            .ToList();

        return new DashboardStatsResponse
        {
            TotalReservas = reservas.Count,
            ReservasActivas = reservas.Count(reserva => reserva.Estado == EstadoReserva.Activa),
            ReservasCanceladas = reservas.Count(reserva => reserva.Estado == EstadoReserva.Cancelada),
            EspacioMasPopular = porEspacio.Count > 0 ? porEspacio[0].NombreEspacio : "Sin reservas",
            ReservasPorEspacio = porEspacio,
            ReservasPorEstado = porEstado
        };
    }

    public async Task ValidateAvailability(
        int spaceId,
        DateOnly fecha,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        int? reservaIdExcluir = null,
        CancellationToken cancellationToken = default)
    {
        ValidarRango(fecha, horaInicio, horaFin);

        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        if (fecha < hoy || (fecha == hoy && horaInicio <= TimeOnly.FromDateTime(ahora)))
            throw new ApiException("No se puede reservar una fecha u hora que ya pasó.", StatusCodes.Status400BadRequest);

        var space = await _context.Spaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == spaceId, cancellationToken);
        if (space is null)
            throw new ApiException("El espacio no existe.", StatusCodes.Status404NotFound);

        if (!space.Disponible)
            throw new ApiException("El espacio no está disponible para reservas.", StatusCodes.Status400BadRequest);

        var dia = (DiaSemana)fecha.DayOfWeek;
        var horarios = await _context.SpaceHorarios
            .AsNoTracking()
            .Where(horario => horario.SpaceId == spaceId && horario.DiaSemana == dia && horario.EstaDisponible)
            .ToListAsync(cancellationToken);

        var cabeEnHorario = horarios.Any(horario => horaInicio >= horario.HoraInicio && horaFin <= horario.HoraFin);
        if (!cabeEnHorario)
        {
            throw new ApiException(
                "El espacio no opera en la franja horaria solicitada. Revisa el día y el horario de funcionamiento.",
                StatusCodes.Status400BadRequest);
        }

        var ocupado = await _context.Reservations.AnyAsync(reserva =>
            reserva.SpaceId == spaceId
            && reserva.Fecha == fecha
            && reserva.Estado == EstadoReserva.Activa
            && (reservaIdExcluir == null || reserva.Id != reservaIdExcluir)
            && reserva.HoraInicio < horaFin
            && reserva.HoraFin > horaInicio, cancellationToken);

        if (ocupado)
            throw new ApiException("El espacio ya está ocupado en esa franja horaria.", StatusCodes.Status400BadRequest);
    }

    private async Task TrySendConfirmationAsync(User user, Reservation reserva)
    {
        try
        {
            await _emailService.SendReservationConfirmationAsync(
                user.Email,
                user.Nombre,
                reserva.Space?.Nombre ?? "Espacio",
                reserva.Fecha,
                reserva.HoraInicio,
                reserva.HoraFin);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "La reserva {ReservaId} quedó guardada, pero no se pudo enviar el correo de confirmación a {Email}.",
                reserva.Id,
                user.Email);
        }
    }

    private async Task EnsureUserExists(int userId, CancellationToken cancellationToken)
    {
        var exists = await _context.Users.AnyAsync(user => user.Id == userId, cancellationToken);
        if (!exists)
            throw new ApiException("El usuario autenticado no existe.", StatusCodes.Status401Unauthorized);
    }

    private static void ValidarRango(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (fecha == default)
            throw new ApiException("La fecha de la reserva es obligatoria.", StatusCodes.Status400BadRequest);

        if (horaFin <= horaInicio)
            throw new ApiException("La hora de fin debe ser mayor que la hora de inicio.", StatusCodes.Status400BadRequest);
    }

    private static ReservationResponse Map(Reservation reservation)
    {
        return new ReservationResponse
        {
            Id = reservation.Id,
            UserId = reservation.UserId,
            NombreUsuario = reservation.User?.Nombre ?? string.Empty,
            SpaceId = reservation.SpaceId,
            NombreEspacio = reservation.Space?.Nombre ?? string.Empty,
            UbicacionEspacio = reservation.Space?.Ubicacion ?? string.Empty,
            Fecha = reservation.Fecha,
            HoraInicio = reservation.HoraInicio,
            HoraFin = reservation.HoraFin,
            Estado = reservation.Estado,
            FechaCreacion = reservation.FechaCreacion
        };
    }
}
