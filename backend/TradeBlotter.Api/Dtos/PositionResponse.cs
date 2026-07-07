using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Dtos;

/// <summary>Outgoing representation of a derived position.</summary>
public record PositionResponse(string Symbol, decimal NetQuantity, decimal AverageCost)
{
    public static PositionResponse From(Position p) =>
        new(p.Symbol, p.NetQuantity, p.AverageCost);
}
