using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCG.Games.Application.Search;

namespace FCG.Games.Application.Abstractions;

public interface IGameSearchRepository
{
    Task IndexAsync(
        GameSearchDocument game,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GameSearchDocument>> SearchAsync(
        string? term,
        string? genre,
        decimal? minimumPrice,
        decimal? maximumPrice,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GameSearchDocument>> GetPopularAsync(
        int quantity,
        CancellationToken cancellationToken = default);

    Task IncrementPurchaseCountAsync(
    int gameId,
    CancellationToken cancellationToken = default);


    Task<IReadOnlyCollection<GameSearchDocument>> RecommendAsync(
    IReadOnlyCollection<string> preferredGenres,
    IReadOnlyCollection<int> ownedGameIds,
    int quantity,
    CancellationToken cancellationToken = default);
}
