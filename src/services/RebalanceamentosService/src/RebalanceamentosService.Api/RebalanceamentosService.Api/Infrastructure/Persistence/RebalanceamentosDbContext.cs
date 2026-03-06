using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RebalanceamentosService.Api.Domain.Entities;
using RebalanceamentosService.Api.Domain.Enums;
using BuildingBlocks.Persistence.Inbox;

namespace RebalanceamentosService.Api.Infrastructure.Persistence;

public sealed class RebalanceamentosDbContext(DbContextOptions<RebalanceamentosDbContext> options)
    : AppDbContextBase(options)
{
    public DbSet<Rebalanceamento> Rebalanceamentos => Set<Rebalanceamento>();
    public DbSet<VendaRebalanceamento> VendasRebalanceamento => Set<VendaRebalanceamento>();
    public DbSet<InboxProcessedEvent> InboxProcessedEvents => Set<InboxProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var tipoConverter = new ValueConverter<TipoRebalanceamento, string>(
            v => v.ToString(),
            v => (TipoRebalanceamento)Enum.Parse(typeof(TipoRebalanceamento), v, true)
        );

        modelBuilder.Entity<Rebalanceamento>(b =>
        {
            b.ToTable("Rebalanceamentos");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId).IsRequired();

            b.Property(x => x.Tipo)
                .HasConversion(tipoConverter)
                .HasMaxLength(30)
                .IsRequired();

            b.Property(x => x.TickerVendido).HasMaxLength(10).IsRequired();
            b.Property(x => x.TickerComprado).HasMaxLength(10).IsRequired();

            b.Property(x => x.ValorVenda).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.DataRebalanceamento).IsRequired();

            b.HasIndex(x => x.ClienteId);
            b.HasIndex(x => x.DataRebalanceamento);
        });

        modelBuilder.Entity<VendaRebalanceamento>(b =>
        {
            b.ToTable("VendasRebalanceamento");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId).IsRequired();
            b.Property(x => x.Ticker).HasMaxLength(10).IsRequired();
            b.Property(x => x.Quantidade).IsRequired();

            b.Property(x => x.PrecoVenda).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.PrecoMedio).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.ValorVenda).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Lucro).HasPrecision(18, 2).IsRequired();

            b.Property(x => x.DataOperacaoUtc).IsRequired();

            b.HasIndex(x => x.ClienteId);
            b.HasIndex(x => x.DataOperacaoUtc);
        });

        modelBuilder.Entity<InboxProcessedEvent>(b =>
        {
            b.ToTable("InboxProcessedEvents");
            b.HasKey(x => x.Id);

            b.Property(x => x.EventId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Consumer).HasMaxLength(100).IsRequired();
            b.Property(x => x.ProcessedAt).IsRequired();

            b.HasIndex(x => new { x.EventId, x.Consumer }).IsUnique();
        });
    }
}