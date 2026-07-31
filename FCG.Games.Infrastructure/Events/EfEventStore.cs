using System.Diagnostics;
using System.Text.Json;
using FCG.Games.Application.Abstractions.Events;
using FCG.Games.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Events;

public sealed class EfEventStore(GamesDbContext db) : IEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task CommitAsync(
        string aggregateType,
        Func<string> aggregateIdFactory,
        string eventType,
        Func<object> dataFactory,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Persiste primeiro as alterações pendentes para obter IDs gerados pelo banco.
        await db.SaveChangesAsync(cancellationToken);

        var aggregateId = aggregateIdFactory();
        var currentVersion = await db.StoredEvents
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0;

        await db.StoredEvents.AddAsync(new StoredEvent
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            Version = currentVersion + 1,
            Data = JsonSerializer.Serialize(dataFactory(), JsonOptions),
            TraceId = Activity.Current?.TraceId.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredEventResponse>> GetStreamAsync(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken = default)
    {
        return await db.StoredEvents
            .AsNoTracking()
            .Where(x => x.AggregateType == aggregateType && x.AggregateId == aggregateId)
            .OrderBy(x => x.Version)
            .Select(x => new StoredEventResponse(
                x.Id,
                x.AggregateType,
                x.AggregateId,
                x.EventType,
                x.Version,
                x.Data,
                x.TraceId,
                x.OccurredAtUtc))
            .ToListAsync(cancellationToken);
    }
}
