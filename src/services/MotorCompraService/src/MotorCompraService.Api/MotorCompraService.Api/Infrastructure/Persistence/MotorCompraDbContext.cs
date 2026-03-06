using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using MotorCompraService.Api.Domain.Entities;

namespace MotorCompraService.Api.Infrastructure.Persistence;

public sealed class MotorCompraDbContext(DbContextOptions<MotorCompraDbContext> options) : AppDbContextBase(options)
{
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();

    public DbSet<Aporte> Aportes => Set<Aporte>();


    public DbSet<CustodiaMasterSaldo> CustodiaMasterSaldos => Set<CustodiaMasterSaldo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustodiaMasterSaldo>(b =>
        {
            b.ToTable("CustodiaMasterSaldos");
            b.HasKey(x => x.Id);
            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();
            b.HasIndex(x => x.Ticker).IsUnique();
            b.Property(x => x.Quantidade).IsRequired();
            b.Property(x => x.AtualizadoEm).IsRequired();
        });

        modelBuilder.Entity<Distribuicao>(b =>
        {
            b.ToTable("Distribuicoes");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId).IsRequired();
            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();
            b.Property(x => x.Quantidade).IsRequired();
            b.Property(x => x.PrecoUnitario).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.DataDistribuicao).IsRequired();

            b.HasOne(x => x.OrdemCompra)
             .WithMany()
             .HasForeignKey(x => x.OrdemCompraId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.ClienteId, x.Ticker, x.DataDistribuicao });
        });

        modelBuilder.Entity<Aporte>(b =>
        {
            b.ToTable("Aportes");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId).IsRequired();
            b.Property(x => x.DataReferencia).IsRequired();
            b.Property(x => x.Valor).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Parcela).HasMaxLength(10).IsRequired();
            b.Property(x => x.CriadoEm).IsRequired();

            b.HasIndex(x => new { x.ClienteId, x.DataReferencia }).IsUnique();
            b.HasIndex(x => x.ClienteId);
        });
    }
}