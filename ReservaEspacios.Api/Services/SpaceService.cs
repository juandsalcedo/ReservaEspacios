using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Api.Data;
using ReservaEspacios.Api.DTOs;
using ReservaEspacios.Api.Exceptions;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Services;

public class SpaceService : ISpaceService
{
    private readonly ReservaEspaciosContext _context;
    private readonly ILogger<SpaceService> _logger;

    public SpaceService(ReservaEspaciosContext context, ILogger<SpaceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SpaceResponse>> GetAllSpaces(CancellationToken cancellationToken = default)
    {
        var spaces = await _context.Spaces
            .AsNoTracking()
            .Include(space => space.Horarios)
            .OrderBy(space => space.Nombre)
            .ToListAsync(cancellationToken);

        return spaces.Select(Map).ToList();
    }

    public async Task<SpaceResponse> GetSpaceById(int id, CancellationToken cancellationToken = default)
    {
        var space = await _context.Spaces
            .AsNoTracking()
            .Include(item => item.Horarios)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (space is null)
            throw new ApiException("No se encontró el espacio.", StatusCodes.Status404NotFound);

        return Map(space);
    }

    public async Task<SpaceResponse> CreateSpace(int userId, CreateSpaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdmin(userId, cancellationToken);
        ValidarDatos(request.Nombre, request.Capacidad, request.Ubicacion, request.Descripcion, request.HorarioFuncionamiento, request.Tipo);

        var nombre = request.Nombre.Trim();
        if (await _context.Spaces.AnyAsync(space => space.Nombre == nombre, cancellationToken))
            throw new ApiException("Ya existe un espacio con ese nombre.", StatusCodes.Status409Conflict);

        var space = new Space
        {
            Nombre = nombre,
            Capacidad = request.Capacidad,
            Ubicacion = request.Ubicacion.Trim(),
            Tipo = request.Tipo,
            Descripcion = request.Descripcion.Trim(),
            HorarioFuncionamiento = request.HorarioFuncionamiento.Trim(),
            Disponible = request.Disponible
        };

        _context.Spaces.Add(space);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Espacio {SpaceId} creado por el administrador {UserId}.", space.Id, userId);
        return Map(space);
    }

    public async Task<SpaceResponse> UpdateSpace(int id, int userId, UpdateSpaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureAdmin(userId, cancellationToken);
        ValidarDatos(request.Nombre, request.Capacidad, request.Ubicacion, request.Descripcion, request.HorarioFuncionamiento, request.Tipo);

        var space = await _context.Spaces
            .Include(item => item.Horarios)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (space is null)
            throw new ApiException("No se encontró el espacio.", StatusCodes.Status404NotFound);

        var nombre = request.Nombre.Trim();
        var duplicado = await _context.Spaces.AnyAsync(item => item.Id != id && item.Nombre == nombre, cancellationToken);
        if (duplicado)
            throw new ApiException("Ya existe un espacio con ese nombre.", StatusCodes.Status409Conflict);

        space.Nombre = nombre;
        space.Capacidad = request.Capacidad;
        space.Ubicacion = request.Ubicacion.Trim();
        space.Tipo = request.Tipo;
        space.Descripcion = request.Descripcion.Trim();
        space.HorarioFuncionamiento = request.HorarioFuncionamiento.Trim();
        space.Disponible = request.Disponible;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Espacio {SpaceId} actualizado por el administrador {UserId}.", space.Id, userId);
        return Map(space);
    }

    public async Task<SpaceResponse> DeleteSpace(int id, int userId, CancellationToken cancellationToken = default)
    {
        await EnsureAdmin(userId, cancellationToken);

        var space = await _context.Spaces
            .Include(item => item.Horarios)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (space is null)
            throw new ApiException("No se encontró el espacio.", StatusCodes.Status404NotFound);

        var tieneReservas = await _context.Reservations.AnyAsync(reserva => reserva.SpaceId == id, cancellationToken);
        if (tieneReservas)
            throw new ApiException("No se puede eliminar el espacio porque tiene reservas asociadas.", StatusCodes.Status400BadRequest);

        var respuesta = Map(space);
        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Espacio {SpaceId} eliminado por el administrador {UserId}.", id, userId);
        return respuesta;
    }

    private async Task EnsureAdmin(int userId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (usuario is null)
            throw new ApiException("El usuario autenticado no existe.", StatusCodes.Status401Unauthorized);
        if (usuario.Rol != RolUsuario.Admin)
            throw new ApiException("Solo un administrador puede crear, editar o eliminar espacios.", StatusCodes.Status403Forbidden);
    }

    private static void ValidarDatos(string nombre, int capacidad, string ubicacion, string descripcion, string horario, TipoEspacio tipo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ApiException("El nombre del espacio es obligatorio.", StatusCodes.Status400BadRequest);
        if (capacidad <= 0)
            throw new ApiException("La capacidad debe ser mayor que cero.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(ubicacion))
            throw new ApiException("La ubicación es obligatoria.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ApiException("La descripción es obligatoria.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(horario))
            throw new ApiException("El horario de funcionamiento es obligatorio.", StatusCodes.Status400BadRequest);
        if (!Enum.IsDefined(tipo))
            throw new ApiException("El tipo de espacio no es válido.", StatusCodes.Status400BadRequest);
    }

    private static SpaceResponse Map(Space space)
    {
        return new SpaceResponse
        {
            Id = space.Id,
            Nombre = space.Nombre,
            Capacidad = space.Capacidad,
            Ubicacion = space.Ubicacion,
            Tipo = space.Tipo,
            Descripcion = space.Descripcion,
            HorarioFuncionamiento = space.HorarioFuncionamiento,
            Disponible = space.Disponible,
            Horarios = space.Horarios
                .OrderBy(horario => horario.DiaSemana)
                .ThenBy(horario => horario.HoraInicio)
                .Select(SpaceHorariosService.Map)
                .ToList()
        };
    }
}
