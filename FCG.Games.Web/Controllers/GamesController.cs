using System.Security.Claims;
using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace FCG.Games.Web.Controllers; 
[ApiController, Route("api/games")] 
public sealed class GamesController(GameService service) : ControllerBase 
{ 
    [AllowAnonymous, HttpGet] 
    public async Task<IActionResult> List() => Ok(await service.ListAsync()); 

    [Authorize(Roles = "Admin"), HttpPost] 
    public async Task<IActionResult> Create(CreateGameRequest r) => Ok(await service.CreateAsync(r)); 

    [Authorize, HttpPost("{gameId:int}/purchase")] 
    public async Task<IActionResult> Purchase(int gameId) => Ok(await service.PurchaseAsync(UserId(), gameId)); 

    [Authorize, HttpGet("library")] 
    public async Task<IActionResult> Library() => Ok(await service.LibraryAsync(UserId())); 

    [Authorize, HttpGet("recommendations")] 
    public async Task<IActionResult> Recommend() => Ok(await service.RecommendAsync(UserId())); 
    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet("search")]
    public async Task<IActionResult> Search(
    [FromQuery] string? term,
    [FromQuery] string? genre,
    [FromQuery] decimal? minimumPrice,
    [FromQuery] decimal? maximumPrice,
    [FromServices] IGameSearchRepository searchRepository,
    CancellationToken cancellationToken)
    {
        var result = await searchRepository.SearchAsync(
            term,
            genre,
            minimumPrice,
            maximumPrice,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> Popular(
    [FromQuery] int quantity,
    [FromServices] IGameSearchRepository searchRepository,
    CancellationToken cancellationToken)
    {
        var result = await searchRepository.GetPopularAsync(
            quantity,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("recommendations/{userId:int}")]
    public async Task<IActionResult> Recommendations(
    int userId,
    [FromQuery] int quantity = 10,
    CancellationToken cancellationToken = default)
    {
        var result = await service.RecommendAsync(
            userId,
            quantity,
            cancellationToken);

        return Ok(result);
    }
}
