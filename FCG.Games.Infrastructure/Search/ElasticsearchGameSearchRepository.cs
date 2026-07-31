using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Search;
using Microsoft.Extensions.Configuration;


namespace FCG.Games.Infrastructure.Search;

public sealed class ElasticsearchGameSearchRepository(
    ElasticsearchClient client,
    IConfiguration configuration)
    : IGameSearchRepository
{
    private readonly string _indexName =
        configuration["Elasticsearch:GamesIndex"]
        ?? "fcg-games";

    public async Task IndexAsync(
        GameSearchDocument game,
        CancellationToken cancellationToken = default)
    {
        var response = await client.IndexAsync(
            game,
            request => request
                .Index(_indexName)
                .Id(game.Id),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            var statusCode = response.ApiCallDetails?.HttpStatusCode;
            var errorType =
                response.ElasticsearchServerError?.Error?.Type;
            var reason =
                response.ElasticsearchServerError?.Error?.Reason;


            throw new InvalidOperationException(
            $"""
            Falha ao indexar o jogo {game.Id}.

            Status HTTP: {statusCode}
            Tipo: {errorType}
            Motivo: {reason}

            Diagnóstico:
            {response.DebugInformation}
            """);
        }




    }

    public async Task<IReadOnlyCollection<GameSearchDocument>> SearchAsync(
        string? term,
        string? genre,
        decimal? minimumPrice,
        decimal? maximumPrice,
        CancellationToken cancellationToken = default)
    {
        var response = await client.SearchAsync<GameSearchDocument>(
            search => search
                .Index(_indexName)
                .Size(50)
                .Query(query => query.Bool(boolean =>
                {
                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        boolean.Must(must => must.MultiMatch(multi => multi
                            .Query(term)
                            .Fields(new[]
                            {
                                "name^3",
                                "genre^2"
                            })));
                    }

                    if (!string.IsNullOrWhiteSpace(genre))
                    {
                        boolean.Filter(filter => filter.Term(termQuery =>
                            termQuery
                                .Field("genre.keyword")
                                .Value(genre)));
                    }

                    if (minimumPrice.HasValue || maximumPrice.HasValue)
                    {
                        boolean.Filter(filter => filter.Range(new NumberRangeQuery
                        {
                            Field = "price",
                            Gte = minimumPrice.HasValue
                                ? (double)minimumPrice.Value
                                : null,
                            Lte = maximumPrice.HasValue
                                ? (double)maximumPrice.Value
                                : null
                        }));
                    }
                })),
            cancellationToken);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                "Erro ao buscar jogos no Elasticsearch.");

        return response.Documents;
    }

    public async Task<IReadOnlyCollection<GameSearchDocument>> GetPopularAsync(
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var response = await client.SearchAsync<GameSearchDocument>(
            search => search
                .Index(_indexName)
                .Size(quantity)
                .Sort(sort => sort
                    .Field(
                        field => field.PurchaseCount,
                        options => options.Order(
                            Elastic.Clients.Elasticsearch.SortOrder.Desc))),
            cancellationToken);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(
                "Erro ao consultar jogos populares.");

        return response.Documents;
    }

    public async Task IncrementPurchaseCountAsync(
    int gameId,
    CancellationToken cancellationToken = default)
    {
        var response = await client.UpdateAsync<GameSearchDocument, object>(
            _indexName,
            gameId,
            update => update
                .Script(script => script
                    .Source("ctx._source.purchaseCount += params.increment")
                    .Params(new Dictionary<string, object>
                    {
                        ["increment"] = 1
                    })),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Erro ao atualizar popularidade do jogo {gameId}.");
        }
    }

    public async Task<IReadOnlyCollection<GameSearchDocument>> RecommendAsync(
    IReadOnlyCollection<string> preferredGenres,
    IReadOnlyCollection<int> ownedGameIds,
    int quantity,
    CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Array.Empty<GameSearchDocument>();

        var shouldQueries = new List<Query>();
        var mustNotQueries = new List<Query>();

        foreach (var genre in preferredGenres
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            shouldQueries.Add(new TermQuery
            {
                Field = "genre.keyword",
                Value = genre,
                Boost = 3
            });
        }

        foreach (var gameId in ownedGameIds.Distinct())
        {
            mustNotQueries.Add(new TermQuery
            {
                Field = "id",
                Value = gameId
            });
        }

        Query query;

        if (shouldQueries.Count == 0 && mustNotQueries.Count == 0)
        {
            query = new MatchAllQuery();
        }
        else
        {
            query = new BoolQuery
            {
                Should = shouldQueries,
                MustNot = mustNotQueries,
                MinimumShouldMatch = shouldQueries.Count > 0 ? 1 : null
            };
        }

        var response = await client.SearchAsync<GameSearchDocument>(
            search => search
                .Index(_indexName)
                .Size(quantity)
                .Query(query)
                .TrackScores(true)
                .Sort(sort => sort
                    .Score(score => score
                        .Order(SortOrder.Desc))
                    .Field(
                        field => field.PurchaseCount,
                        options => options
                            .Order(SortOrder.Desc))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Erro ao recomendar jogos: {response.DebugInformation}");
        }

        return response.Documents;
    }
}