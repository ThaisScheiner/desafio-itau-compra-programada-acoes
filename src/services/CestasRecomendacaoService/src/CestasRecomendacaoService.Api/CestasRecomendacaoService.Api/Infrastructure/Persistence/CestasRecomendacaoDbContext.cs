using BuildingBlocks.Persistence;
using CestasRecomendacaoService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CestasRecomendacaoService.Api.Infrastructure.Persistence;

public sealed class CestasRecomendacaoDbContext(DbContextOptions<CestasRecomendacaoDbContext> options) : AppDbContextBase(options)
{
    public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
    public DbSet<ItemCesta> ItensCesta => Set<ItemCesta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CestaRecomendacao>(b =>
        {
            b.ToTable("CestasRecomendacao");
            b.HasKey(x => x.Id);

            b.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            b.Property(x => x.Ativa).IsRequired();
            b.Property(x => x.DataCriacao).IsRequired();
            b.Property(x => x.DataDesativacao);

            b.HasMany(x => x.Itens)
             .WithOne(x => x.Cesta)
             .HasForeignKey(x => x.CestaId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.Ativa);
        });

        modelBuilder.Entity<ItemCesta>(b =>
        {
            b.ToTable("ItensCesta");
            b.HasKey(x => x.Id);

            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();
            b.Property(x => x.Percentual).HasPrecision(5, 2).IsRequired();

            b.HasIndex(x => new { x.CestaId, x.Ticker }).IsUnique();
        });
    }
}