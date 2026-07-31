using FCG.Games.Application.Abstractions.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Games.Web.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventsController(IEventStore eventStore) : ControllerBase
{
    [HttpGet("{aggregateType}/{aggregateId}")]
    public async Task<ActionResult<IReadOnlyList<StoredEventResponse>>> GetStream(
        string aggregateType,
        string aggregateId,
        CancellationToken cancellationToken)
    {
        var events = await eventStore.GetStreamAsync(
            aggregateType,
            aggregateId,
            cancellationToken);

        return Ok(events);
    }
}
