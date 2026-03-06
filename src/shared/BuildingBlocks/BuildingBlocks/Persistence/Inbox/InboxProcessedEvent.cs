namespace BuildingBlocks.Persistence.Inbox;

public sealed class InboxProcessedEvent
{
    public long Id { get; set; }
    public string EventId { get; set; } = string.Empty; 
    public string Consumer { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}