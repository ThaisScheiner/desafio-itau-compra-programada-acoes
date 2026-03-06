namespace BuildingBlocks.Persistence.Outbox;

public sealed class OutboxEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}