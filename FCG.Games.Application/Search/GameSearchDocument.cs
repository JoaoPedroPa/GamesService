using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCG.Games.Application.Search;

public sealed class GameSearchDocument
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Genre { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int PurchaseCount { get; init; }
}
