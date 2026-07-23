using System.Security.Claims;
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
}
