using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Data;

public static class DbSeeder
{
    public const string DemoPassword = "Cun2026*";
    public const string AdminEmail = "admin@cun.edu.co";
    public const string AdminPassword = "AdminCun2026*";
    public const string HorarioTexto = "Lunes a viernes 07:00-22:00; sábados 08:00-18:00";

    private static readonly (string Nombre, int Capacidad, string Ubicacion, TipoEspacio Tipo, string Descripcion)[] Espacios =
    [
        ("MESA DE PING PONG", 2, "Piso 1", TipoEspacio.Ludico, "Mesa de ping pong para entretenimiento"),
        ("LABORATORIO DE FÍSICA", 20, "Piso 3", TipoEspacio.Laboratorio, "Laboratorio con equipos de física"),
        ("AUDITORIO", 100, "Piso 2", TipoEspacio.Auditorio, "Auditorio principal para conferencias"),
        ("SALA DE SISTEMAS 1", 30, "Piso 1", TipoEspacio.Aula, "Sala de sistemas con equipos de alta gama")
    ];

    public static async Task SeedAsync(ReservaEspaciosContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await EnsureAdminAsync(db, cancellationToken);
        await UpgradeLegacyPasswordsAsync(db, cancellationToken);
        var creados = await EnsureSpacesAsync(db, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Datos iniciales listos. Administrador: {AdminEmail}. Espacios universitarios asegurados: {Cantidad}.",
            AdminEmail,
            creados);
    }

    private static async Task EnsureAdminAsync(ReservaEspaciosContext db, CancellationToken cancellationToken)
    {
        var admin = await db.Users.FirstOrDefaultAsync(user => user.Email == AdminEmail, cancellationToken);
        if (admin is not null)
        {
            if (admin.Rol != RolUsuario.Admin)
                admin.Rol = RolUsuario.Admin;
            return;
        }

        db.Users.Add(new User
        {
            Nombre = "Administrador CUN",
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            Rol = RolUsuario.Admin,
            Telefono = "3105550100",
            Facultad = "Administración de campus",
            FechaRegistro = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpgradeLegacyPasswordsAsync(ReservaEspaciosContext db, CancellationToken cancellationToken)
    {
        var correos = new[] { "ana.gomez@cun.edu.co", "luis.rojas@cun.edu.co" };
        var usuarios = await db.Users
            .Where(user => correos.Contains(user.Email) && !user.PasswordHash.StartsWith("$2"))
            .ToListAsync(cancellationToken);

        foreach (var usuario in usuarios)
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        if (usuarios.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> EnsureSpacesAsync(ReservaEspaciosContext db, CancellationToken cancellationToken)
    {
        var asegurados = 0;
        foreach (var spec in Espacios)
        {
            var space = await db.Spaces.FirstOrDefaultAsync(item => item.Nombre == spec.Nombre, cancellationToken);
            if (space is null)
            {
                space = new Space
                {
                    Nombre = spec.Nombre,
                    Capacidad = spec.Capacidad,
                    Ubicacion = spec.Ubicacion,
                    Tipo = spec.Tipo,
                    Descripcion = spec.Descripcion,
                    HorarioFuncionamiento = HorarioTexto,
                    Disponible = true
                };
                db.Spaces.Add(space);
                await db.SaveChangesAsync(cancellationToken);
            }

            var tieneHorario = await db.SpaceHorarios.AnyAsync(horario => horario.SpaceId == space.Id, cancellationToken);
            if (!tieneHorario)
            {
                db.SpaceHorarios.AddRange(CrearHorarios(space.Id));
                await db.SaveChangesAsync(cancellationToken);
            }

            asegurados++;
        }

        return asegurados;
    }

    private static IEnumerable<SpaceHorario> CrearHorarios(int spaceId)
    {
        DiaSemana[] entreSemana = [DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles, DiaSemana.Jueves, DiaSemana.Viernes];
        foreach (var dia in entreSemana)
        {
            yield return new SpaceHorario
            {
                SpaceId = spaceId,
                DiaSemana = dia,
                HoraInicio = new TimeOnly(7, 0),
                HoraFin = new TimeOnly(22, 0),
                EstaDisponible = true
            };
        }

        yield return new SpaceHorario
        {
            SpaceId = spaceId,
            DiaSemana = DiaSemana.Sabado,
            HoraInicio = new TimeOnly(8, 0),
            HoraFin = new TimeOnly(18, 0),
            EstaDisponible = true
        };
    }
}
