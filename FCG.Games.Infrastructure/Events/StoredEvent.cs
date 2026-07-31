namespace FCG.Games.Infrastructure.Events;

public sealed class StoredEvent
{
    public Guid Id { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Data { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
