using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class SpaceHorariosService : ISpaceHorariosService
{
    private readonly ReservaEspaciosContext _context;
    private readonly ILogger<SpaceHorariosService> _logger;

    public SpaceHorariosService(ReservaEspaciosContext context, ILogger<SpaceHorariosService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SpaceHorarioResponse>> GetAll(int? spaceId, CancellationToken cancellationToken = default)
    {
        var query = _context.SpaceHorarios
            .AsNoTracking()
            .Include(horario => horario.Space)
            .AsQueryable();

        if (spaceId is > 0)
            query = query.Where(horario => horario.SpaceId == spaceId);

        var horarios = await query
            .OrderBy(horario => horario.SpaceId)
            .ThenBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .ToListAsync(cancellationToken);

        return horarios.Select(Map).ToList();
    }

    public async Task<SpaceHorarioResponse> GetById(int id, CancellationToken cancellationToken = default)
    {
        var horario = await _context.SpaceHorarios
            .AsNoTracking()
            .Include(item => item.Space)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (horario is null)
            throw new ApiException("No se encontró el horario.", StatusCodes.Status404NotFound);

        return Map(horario);
    }

    public async Task<SpaceHorarioResponse> Create(int userId, CreateSpaceHorarioRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdmin(userId, cancellationToken);
        ValidarRango(request.HoraInicio, request.HoraFin);
        ValidarDia(request.DiaSemana);

        if (request.SpaceId <= 0)
            throw new ApiException("El espacio es obligatorio.", StatusCodes.Status400BadRequest);

        var space = await _context.Spaces.FirstOrDefaultAsync(item => item.Id == request.SpaceId, cancellationToken);
        if (space is null)
            throw new ApiException("El espacio no existe.", StatusCodes.Status404NotFound);

        var horario = new SpaceHorario
        {
            SpaceId = space.Id,
            Space = space,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            EstaDisponible = request.EstaDisponible
        };

        _context.SpaceHorarios.Add(horario);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Horario {HorarioId} creado para el espacio {SpaceId}.", horario.Id, space.Id);
        return Map(horario);
    }

    public async Task<SpaceHorarioResponse> Update(int id, int userId, UpdateSpaceHorarioRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdmin(userId, cancellationToken);
        ValidarRango(request.HoraInicio, request.HoraFin);
        ValidarDia(request.DiaSemana);

        var horario = await _context.SpaceHorarios
            .Include(item => item.Space)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (horario is null)
            throw new ApiException("No se encontró el horario.", StatusCodes.Status404NotFound);

        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFin = request.HoraFin;
        horario.EstaDisponible = request.EstaDisponible;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Horario {HorarioId} actualizado por el administrador {UserId}.", horario.Id, userId);
        return Map(horario);
    }

    public async Task<SpaceHorarioResponse> Delete(int id, int userId, CancellationToken cancellationToken = default)
    {
        await EnsureAdmin(userId, cancellationToken);

        var horario = await _context.SpaceHorarios
            .Include(item => item.Space)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (horario is null)
            throw new ApiException("No se encontró el horario.", StatusCodes.Status404NotFound);

        var respuesta = Map(horario);
        _context.SpaceHorarios.Remove(horario);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Horario {HorarioId} eliminado por el administrador {UserId}.", id, userId);
        return respuesta;
    }

    internal static SpaceHorarioResponse Map(SpaceHorario horario)
    {
        return new SpaceHorarioResponse
        {
            Id = horario.Id,
            SpaceId = horario.SpaceId,
            NombreEspacio = horario.Space?.Nombre,
            DiaSemana = horario.DiaSemana,
            HoraInicio = horario.HoraInicio,
            HoraFin = horario.HoraFin,
            EstaDisponible = horario.EstaDisponible
        };
    }

    internal static void ValidarRango(TimeOnly horaInicio, TimeOnly horaFin)
    {
        if (horaFin <= horaInicio)
            throw new ApiException("La hora de fin debe ser mayor que la hora de inicio.", StatusCodes.Status400BadRequest);
    }

    private async Task EnsureAdmin(int userId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (usuario is null)
            throw new ApiException("El usuario autenticado no existe.", StatusCodes.Status401Unauthorized);
        if (usuario.Rol != RolUsuario.Admin)
            throw new ApiException("Solo un administrador puede crear, editar o eliminar horarios.", StatusCodes.Status403Forbidden);
    }

    private static void ValidarDia(DiaSemana dia)
    {
        if (!Enum.IsDefined(dia))
            throw new ApiException("El día de la semana no es válido.", StatusCodes.Status400BadRequest);
    }
}
