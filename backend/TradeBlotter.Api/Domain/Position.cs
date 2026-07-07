namespace TradeBlotter.Api.Domain;

/// <summary>
/// A derived (never persisted) net position for a symbol, computed by
/// <see cref="PositionCalculator"/> from the trade history.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="NetQuantity">Signed net shares held: positive = long, negative = short.</param>
/// <param name="AverageCost">Weighted-average cost of the currently-open position.</param>
public record Position(string Symbol, decimal NetQuantity, decimal AverageCost);
