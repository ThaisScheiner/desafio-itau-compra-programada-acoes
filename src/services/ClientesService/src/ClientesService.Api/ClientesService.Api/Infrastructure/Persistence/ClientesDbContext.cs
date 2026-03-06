using BuildingBlocks.Persistence;
using ClientesService.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientesService.Api.Infrastructure.Persistence;

public sealed class ClientesDbContext(DbContextOptions<ClientesDbContext> options)
    : AppDbContextBase(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(b =>
        {
            b.ToTable("Clientes");
            b.HasKey(x => x.Id);

            b.Property(x => x.Nome)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(x => x.CPF)
                .HasMaxLength(11)
                .IsRequired();

            b.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(x => x.ValorMensal)
                .HasPrecision(18, 2)
                .IsRequired();

            b.Property(x => x.Ativo)
                .IsRequired()
                .HasDefaultValue(true);

            b.Property(x => x.DataAdesao)
                .IsRequired();

            b.Property(x => x.DataSaida)
                .IsRequired(false);

            b.HasIndex(x => x.CPF).IsUnique();

            b.HasOne(x => x.ContaGrafica)
                .WithOne(x => x.Cliente)
                .HasForeignKey<ContaGrafica>(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContaGrafica>(b =>
        {
            b.ToTable("ContasGraficas");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClienteId)
                .IsRequired();

            b.Property(x => x.NumeroConta)
                .HasMaxLength(20)
                .IsRequired();

            b.Property(x => x.Tipo)
                .HasMaxLength(10)
                .IsRequired();

            b.Property(x => x.DataCriacao)
                .IsRequired();

            b.HasIndex(x => x.NumeroConta).IsUnique();
            b.HasIndex(x => x.ClienteId).IsUnique(); 
        });
    }
}