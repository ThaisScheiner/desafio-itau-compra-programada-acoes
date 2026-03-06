using BuildingBlocks.Persistence.Inbox;
using BuildingBlocks.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Persistence;

public abstract class AppDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<InboxProcessedEvent> InboxProcessedEvents => Set<InboxProcessedEvent>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InboxProcessedEvent>(b =>
        {
            b.ToTable("InboxProcessedEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Consumer).HasMaxLength(100).IsRequired();
            b.HasIndex(x => new { x.EventId, x.Consumer }).IsUnique();
            b.Property(x => x.ProcessedAt).IsRequired();
        });

        modelBuilder.Entity<OutboxEvent>(b =>
        {
            b.ToTable("OutboxEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            b.Property(x => x.Payload).HasColumnType("LONGTEXT").IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.PublishedAt);
            b.HasIndex(x => x.PublishedAt);
        });
    }
}