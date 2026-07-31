namespace FCG.Games.Application.Abstractions.Events;

public sealed record StoredEventResponse(
    Guid Id,
    string AggregateType,
    string AggregateId,
    string EventType,
    int Version,
    string Data,
    string? TraceId,
    DateTime OccurredAtUtc);

public interface IEventStore
{
    Task CommitAsync(
        string aggregateType,
        Func<string> aggregateIdFactory,
        string eventType,
        Func<object> dataFactory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredEventResponse>> GetStreamAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default);
}
