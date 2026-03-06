using BuildingBlocks.Persistence;
using EventosIRService.Api.Domain.Entities;
using EventosIRService.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EventosIRService.Api.Infrastructure.Persistence;

public sealed class EventosIRDbContext(DbContextOptions<EventosIRDbContext> options) : AppDbContextBase(options)
{
    public DbSet<EventoIR> EventosIR => Set<EventoIR>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Converte enum <-> string 
        var tipoConverter = new ValueConverter<TipoEventoIR, string>(
            v => v.ToString(),
            v => (TipoEventoIR)Enum.Parse(typeof(TipoEventoIR), v, true) 
         );

        modelBuilder.Entity<EventoIR>(b =>
        {
            b.ToTable("EventosIR");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId).IsRequired();

            b.Property(x => x.Tipo)
                .HasConversion(tipoConverter)
                .HasMaxLength(30)
                .IsRequired();

            b.Property(x => x.Ticker).HasMaxLength(12).IsRequired();

            b.Property(x => x.Quantidade).IsRequired();

            b.Property(x => x.PrecoUnitario).HasPrecision(18, 4).IsRequired();
            b.Property(x => x.ValorBase).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.ValorIR).HasPrecision(18, 2).IsRequired();

            b.Property(x => x.PublicadoKafka).IsRequired();
            b.Property(x => x.DataEvento).IsRequired();

            // Idempotencia: Cliente + Tipo + Ticker + DataEvento
            b.HasIndex(x => new { x.ClienteId, x.Tipo, x.Ticker, x.DataEvento })
             .IsUnique();
        });
    }
}