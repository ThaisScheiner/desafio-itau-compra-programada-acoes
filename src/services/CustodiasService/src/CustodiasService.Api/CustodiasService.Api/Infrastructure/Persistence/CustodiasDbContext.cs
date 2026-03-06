using BuildingBlocks.Persistence;
using CustodiasService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustodiasService.Api.Infrastructure.Persistence;

public sealed class CustodiasDbContext(DbContextOptions<CustodiasDbContext> options) : AppDbContextBase(options)
{
    public DbSet<Custodia> Custodias => Set<Custodia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Custodia>(b =>
        {
            b.ToTable("Custodias");
            b.HasKey(x => x.Id);

            b.Property(x => x.TipoConta).HasMaxLength(10).IsRequired();
            b.Property(x => x.ClienteId);
            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();

            b.Property(x => x.Quantidade).IsRequired();
            b.Property(x => x.PrecoMedio).HasPrecision(18, 4).IsRequired();

            b.Property(x => x.AtualizadoEm).IsRequired();

            b.HasIndex(x => new { x.TipoConta, x.ClienteId, x.Ticker }).IsUnique();
            b.HasIndex(x => x.Ticker);
        });
    }
}