using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RebalanceamentosService.Api.Infrastructure.Persistence;

public sealed class RebalanceamentosDbContextFactory : IDesignTimeDbContextFactory<RebalanceamentosDbContext>
{
    public RebalanceamentosDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("MySql")
                 ?? throw new InvalidOperationException("ConnectionStrings:MySql não configurada.");

        var options = new DbContextOptionsBuilder<RebalanceamentosDbContext>()
            .UseMySql(cs, ServerVersion.AutoDetect(cs))
            .Options;

        return new RebalanceamentosDbContext(options);
    }
}