using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Abstractions.Events;
using FCG.Games.Application.Search;
using FCG.Games.Domain.Entities;

namespace FCG.Games.Application.Games;

public sealed record CreateGameRequest(string Name, string Genre, decimal Price);



// teste CI GitHub Actions teste8
public sealed record PurchaseGameResponse(
    Guid PurchaseId,
    Guid PaymentId,
    string Status,
    decimal OriginalAmount,
    decimal DiscountPercentage,
    decimal FinalAmount,
    string? FailureReason);

public sealed class GameService(
    IGamesRepository repo,
    IPaymentsClient payments,
    IGameSearchRepository searchRepository,
    IEventStore eventStore)
{
    public async Task<Game> CreateAsync(CreateGameRequest request)
    {
        var game = Game.Create(request.Name, request.Genre, request.Price);
        await repo.AddGameAsync(game);

        await eventStore.CommitAsync(
            "Game",
            () => game.Id.ToString(),
            "GameCreated",
            () => new
            {
                game.Id,
                game.Name,
                game.Genre,
                game.Price,
                game.Active,
                game.CreatedAtUtc
            });

        await searchRepository.IndexAsync(new GameSearchDocument
        {
            Id = game.Id,
            Name = game.Name,
            Genre = game.Genre,
            Price = game.Price,
            PurchaseCount = 0
        });

        return game;
    }

    public Task<List<Game>> ListAsync() => repo.GetGamesAsync();

    public Task<List<Game>> LibraryAsync(int userId) => repo.GetLibraryAsync(userId);

    public async Task<PurchaseGameResponse> PurchaseAsync(int userId, int gameId)
    {
        var game = await repo.GetGameAsync(gameId)
            ?? throw new KeyNotFoundException("Jogo não encontrado.");

        if (!game.Active)
            throw new InvalidOperationException("Jogo indisponível.");

        if (await repo.OwnsAsync(userId, gameId))
            throw new InvalidOperationException("O usuário já possui esse jogo.");

        var libraryCount = await repo.LibraryCountAsync(userId);
        var purchase = Purchase.Create(userId, gameId, game.Price, libraryCount);

        await repo.AddPurchaseAsync(purchase);

        await eventStore.CommitAsync(
            "Purchase",
            () => purchase.Id.ToString(),
            "PurchaseCreated",
            () => new
            {
                purchase.Id,
                purchase.UserId,
                purchase.GameId,
                purchase.OriginalAmount,
                purchase.DiscountPercentage,
                purchase.FinalAmount,
                Status = purchase.Status.ToString(),
                purchase.CreatedAtUtc
            });

        var result = await payments.ProcessAsync(
            purchase.Id,
            userId,
            gameId,
            purchase.FinalAmount);

        if (result.Status == "Approved")
        {
            purchase.Approve(result.PaymentId);
            await repo.AddLibraryItemAsync(LibraryItem.Create(userId, gameId));
        }
        else
        {
            purchase.Fail(result.PaymentId);
        }

        await eventStore.CommitAsync(
            "Purchase",
            () => purchase.Id.ToString(),
            result.Status == "Approved" ? "PurchaseApproved" : "PurchaseFailed",
            () => new
            {
                purchase.Id,
                purchase.PaymentId,
                Status = purchase.Status.ToString(),
                result.FailureReason,
                LibraryItemCreated = result.Status == "Approved"
            });

        return new PurchaseGameResponse(
            purchase.Id,
            result.PaymentId,
            result.Status,
            purchase.OriginalAmount,
            purchase.DiscountPercentage,
            purchase.FinalAmount,
            result.FailureReason);
    }

    public async Task<List<Game>> RecommendAsync(int userId)
    {
        var library = await repo.GetLibraryAsync(userId);
        var owned = library.Select(x => x.Id).ToHashSet();
        var favorite = library
            .GroupBy(x => x.Genre)
            .OrderByDescending(x => x.Count())
            .FirstOrDefault()?.Key;

        return (await repo.GetGamesAsync())
            .Where(x => !owned.Contains(x.Id) && (favorite is null || x.Genre == favorite))
            .Take(5)
            .ToList();
    }

    public async Task<IReadOnlyCollection<GameSearchDocument>> RecommendAsync(
        int userId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var ownedGames = await repo.GetLibraryAsync(userId);
        var ownedIds = ownedGames.Select(game => game.Id).ToArray();
        var preferredGenres = ownedGames
            .GroupBy(game => game.Genre)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .Take(3)
            .ToArray();

        return await searchRepository.RecommendAsync(
            preferredGenres,
            ownedIds,
            quantity,
            cancellationToken);
    }
}
