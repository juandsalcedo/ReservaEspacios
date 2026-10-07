using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ReservaEspacios.Api.Data;

public class ReservaEspaciosContextFactory : IDesignTimeDbContextFactory<ReservaEspaciosContext>
{
    public ReservaEspaciosContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("ReservaEspaciosDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ReservaEspaciosDb'.");

        var options = new DbContextOptionsBuilder<ReservaEspaciosContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ReservaEspaciosContext(options);
    }
}
