using FCG.Games.Application.Abstractions;
using FCG.Games.Domain.Entities;
namespace FCG.Games.Application.Games;
public sealed record CreateGameRequest(string Name, string Genre, decimal Price);
public sealed record PurchaseGameResponse(Guid PurchaseId, Guid PaymentId, string Status, decimal OriginalAmount, decimal DiscountPercentage, decimal FinalAmount, string? FailureReason);
public sealed class GameService(IGamesRepository repo, IPaymentsClient payments)
{
    public async Task<Game> CreateAsync(CreateGameRequest r)
    {
        var g = Game.Create(r.Name, r.Genre, r.Price);

        await repo.AddGameAsync(g); 

        await repo.SaveAsync(); 

        return g;
    }
    public Task<List<Game>> ListAsync() => repo.GetGamesAsync();
    public Task<List<Game>> LibraryAsync(int userId) => repo.GetLibraryAsync(userId);
    public async Task<PurchaseGameResponse> PurchaseAsync(int userId, int gameId)
    {
        var game = await repo.GetGameAsync(gameId) ?? throw new KeyNotFoundException("Jogo não encontrado.");

        if (!game.Active)
            throw new InvalidOperationException("Jogo indisponível.");

        if (await repo.OwnsAsync(userId, gameId))
            throw new InvalidOperationException("O usuário já possui esse jogo.");

        var count = await repo.LibraryCountAsync(userId);
        var purchase = Purchase.Create(userId, gameId, game.Price, count);

        await repo.AddPurchaseAsync(purchase); 
        await repo.SaveAsync();

        var result = await payments.ProcessAsync(purchase.Id, userId, gameId, purchase.FinalAmount);
        if (result.Status == "Approved")
        {
            purchase.Approve(result.PaymentId);
            await repo.AddLibraryItemAsync(LibraryItem.Create(userId, gameId));
        }

        else
            purchase.Fail(result.PaymentId);
        await repo.SaveAsync(); 
        return new(purchase.Id, result.PaymentId, result.Status, purchase.OriginalAmount, purchase.DiscountPercentage, purchase.FinalAmount, result.FailureReason);
    }
    public async Task<List<Game>> RecommendAsync(int userId)
    {
        var library = await repo.GetLibraryAsync(userId);

        var owned = library.Select(x => x.Id).ToHashSet();

        var favorite = library.GroupBy(x => x.Genre).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key;

        return (await repo.GetGamesAsync()).Where(x => !owned.Contains(x.Id) && (favorite is null || x.Genre == favorite)).Take(5).ToList();
    }
}
