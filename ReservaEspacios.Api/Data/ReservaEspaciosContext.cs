using Microsoft.EntityFrameworkCore;
using ReservaEspacios.Api.Models;

namespace ReservaEspacios.Api.Data;

public class ReservaEspaciosContext : DbContext
{
    public ReservaEspaciosContext(DbContextOptions<ReservaEspaciosContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<SpaceHorario> SpaceHorarios => Set<SpaceHorario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(200).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(128).IsRequired();
            entity.Property(user => user.Telefono).HasMaxLength(30).IsRequired();
            entity.Property(user => user.Facultad).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(user => user.FechaRegistro).HasColumnType("datetime2");
            entity.HasIndex(user => user.Email).IsUnique();

            entity.HasMany(user => user.Reservations)
                .WithOne(reservation => reservation.User)
                .HasForeignKey(reservation => reservation.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Space>(entity =>
        {
            entity.ToTable("Spaces", table =>
                table.HasCheckConstraint("CK_Spaces_Capacidad", "[Capacidad] > 0"));
            entity.HasKey(space => space.Id);
            entity.Property(space => space.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(space => space.Ubicacion).HasMaxLength(200).IsRequired();
            entity.Property(space => space.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(space => space.Descripcion).HasMaxLength(1000).IsRequired();
            entity.Property(space => space.HorarioFuncionamiento).HasMaxLength(200).IsRequired();
            entity.Property(space => space.Disponible).HasDefaultValue(true);

            entity.HasMany(space => space.Reservations)
                .WithOne(reservation => reservation.Space)
                .HasForeignKey(reservation => reservation.SpaceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(space => space.Horarios)
                .WithOne(horario => horario.Space)
                .HasForeignKey(horario => horario.SpaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("Reservations", table =>
                table.HasCheckConstraint("CK_Reservations_RangoHorario", "[HoraFin] > [HoraInicio]"));
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(reservation => reservation.FechaCreacion).HasColumnType("datetime2");
            entity.HasIndex(reservation => reservation.UserId);
            entity.HasIndex(reservation => new { reservation.SpaceId, reservation.Fecha });
        });

        modelBuilder.Entity<SpaceHorario>(entity =>
        {
            entity.ToTable("SpaceHorarios", table =>
                table.HasCheckConstraint("CK_SpaceHorarios_RangoHorario", "[HoraFin] > [HoraInicio]"));
            entity.HasKey(horario => horario.Id);
            entity.Property(horario => horario.DiaSemana).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(horario => horario.EstaDisponible).HasDefaultValue(true);
            entity.HasIndex(horario => new { horario.SpaceId, horario.DiaSemana });
        });
    }
}
