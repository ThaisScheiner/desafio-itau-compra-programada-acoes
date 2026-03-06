using BuildingBlocks.Persistence;
using CotacoesService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CotacoesService.Api.Infrastructure.Persistence;

public sealed class CotacoesDbContext(DbContextOptions<CotacoesDbContext> options) : AppDbContextBase(options)
{
    public DbSet<Cotacao> Cotacoes => Set<Cotacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cotacao>(b =>
        {
            b.ToTable("Cotacoes");
            b.HasKey(x => x.Id);

            b.Property(x => x.DataPregao).IsRequired();
            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();

            b.Property(x => x.PrecoAbertura).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.PrecoFechamento).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.PrecoMaximo).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.PrecoMinimo).HasPrecision(18, 4).IsRequired();

            b.HasIndex(x => new { x.Ticker, x.DataPregao }).IsUnique();
            b.HasIndex(x => x.DataPregao);
        });
    }
}