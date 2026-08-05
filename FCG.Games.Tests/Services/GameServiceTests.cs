using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Abstractions.Events;
using FCG.Games.Application.Games;
using FCG.Games.Application.Search;
using FCG.Games.Domain.Entities;
using Moq;
using Xunit;

namespace FCG.Games.Tests.Services;

public class GameServiceTests
{
    private readonly Mock<IGamesRepository> _repositoryMock;
    private readonly Mock<IPaymentsClient> _paymentsClientMock;
    private readonly Mock<IGameSearchRepository> _searchRepositoryMock;
    private readonly Mock<IEventStore> _eventStoreMock;

    private readonly GameService _gameService;

    public GameServiceTests()
    {
        _repositoryMock = new Mock<IGamesRepository>();
        _paymentsClientMock = new Mock<IPaymentsClient>();
        _searchRepositoryMock = new Mock<IGameSearchRepository>();
        _eventStoreMock = new Mock<IEventStore>();

        _gameService = new GameService(
            _repositoryMock.Object,
            _paymentsClientMock.Object,
            _searchRepositoryMock.Object,
            _eventStoreMock.Object
        );
    }

    [Fact]
    public async Task CreateAsync_QuandoDadosSaoValidos_DeveCriarJogo()
    {
        // Arrange
        var request = new CreateGameRequest(
            "The Witcher 3",
            "RPG",
            99.90m
        );

        Game? gameAdicionado = null;

        _repositoryMock
            .Setup(repository =>
                repository.AddGameAsync(It.IsAny<Game>()))
            .Callback<Game>(game => gameAdicionado = game)
            .Returns(Task.CompletedTask);

        _eventStoreMock
            .Setup(eventStore =>
                eventStore.CommitAsync(
                    "Game",
                    It.IsAny<Func<string>>(),
                    "GameCreated",
                    It.IsAny<Func<object>>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.CompletedTask);

        _searchRepositoryMock
            .Setup(repository =>
                repository.IndexAsync(
                    It.IsAny<GameSearchDocument>(),
                    It.IsAny<CancellationToken>()
                ))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _gameService.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("The Witcher 3", result.Name);
        Assert.Equal("RPG", result.Genre);
        Assert.Equal(99.90m, result.Price);
        Assert.True(result.Active);

        Assert.Same(gameAdicionado, result);

        _repositoryMock.Verify(
            repository => repository.AddGameAsync(result),
            Times.Once
        );

        _eventStoreMock.Verify(
            eventStore =>
                eventStore.CommitAsync(
                    "Game",
                    It.IsAny<Func<string>>(),
                    "GameCreated",
                    It.IsAny<Func<object>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _searchRepositoryMock.Verify(
            repository =>
                repository.IndexAsync(
                    It.Is<GameSearchDocument>(document =>
                        document.Name == "The Witcher 3" &&
                        document.Genre == "RPG" &&
                        document.Price == 99.90m
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_QuandoNomeEstaVazio_DeveLancarExcecao()
    {
        // Arrange
        var request = new CreateGameRequest(
            "",
            "RPG",
            50m
        );

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameService.CreateAsync(request)
        );

        // Assert
        Assert.Equal(
            "Nome é obrigatório.",
            exception.Message
        );

        _repositoryMock.Verify(
            repository =>
                repository.AddGameAsync(It.IsAny<Game>()),
            Times.Never
        );

        _eventStoreMock.Verify(
            eventStore =>
                eventStore.CommitAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<object>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

        _searchRepositoryMock.Verify(
            repository =>
                repository.IndexAsync(
                    It.IsAny<GameSearchDocument>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task CreateAsync_QuandoPrecoEhNegativo_DeveLancarExcecao()
    {
        // Arrange
        var request = new CreateGameRequest(
            "Red Dead Redemption 2",
            "Ação",
            -1m
        );

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _gameService.CreateAsync(request)
        );

        // Assert
        Assert.Equal(
            "Preço não pode ser negativo.",
            exception.Message
        );

        _repositoryMock.Verify(
            repository =>
                repository.AddGameAsync(It.IsAny<Game>()),
            Times.Never
        );

        _eventStoreMock.Verify(
            eventStore =>
                eventStore.CommitAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<object>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );

        _searchRepositoryMock.Verify(
            repository =>
                repository.IndexAsync(
                    It.IsAny<GameSearchDocument>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ListAsync_QuandoExistemJogos_DeveRetornarLista()
    {
        // Arrange
        var games = new List<Game>
        {
            Game.Create(
                "The Witcher 3",
                "RPG",
                99.90m
            ),

            Game.Create(
                "Red Dead Redemption 2",
                "Ação",
                149.90m
            )
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetGamesAsync())
            .ReturnsAsync(games);

        // Act
        var result = await _gameService.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(
            "The Witcher 3",
            result[0].Name
        );

        Assert.Equal(
            "Red Dead Redemption 2",
            result[1].Name
        );

        _repositoryMock.Verify(
            repository => repository.GetGamesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task ListAsync_QuandoNaoExistemJogos_DeveRetornarListaVazia()
    {
        // Arrange
        _repositoryMock
            .Setup(repository =>
                repository.GetGamesAsync())
            .ReturnsAsync(new List<Game>());

        // Act
        var result = await _gameService.ListAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _repositoryMock.Verify(
            repository => repository.GetGamesAsync(),
            Times.Once
        );
    }
}