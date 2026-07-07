namespace TradeBlotter.Api.Domain;

/// <summary>
/// Derives net positions and average cost per symbol from a trade history.
///
/// The average-cost method (with reset on sign flip) works as follows, replaying a
/// symbol's trades oldest-to-newest while tracking a signed net quantity and an
/// average cost. Let <c>signed</c> be the incoming trade's signed quantity:
///
///   1. Opening from flat (net == 0): net = signed; avg = price.
///   2. Increasing exposure (net and signed share a sign): weighted-average the new
///      fill in — avg = (avg*|net| + price*|signed|) / (|net| + |signed|); net += signed.
///   3. Reducing exposure without crossing zero (opposite signs, |signed| <= |net|):
///      net += signed; avg is UNCHANGED (the closed portion realizes P&L, which the
///      open position's average cost does not reflect). If net hits exactly 0, the
///      symbol is flat.
///   4. Flipping through zero (opposite signs, |signed| > |net|): the trade fully
///      closes the old side and opens a new one on the other side; net += signed and
///      avg RESETS to the flipping trade's price (the residual lot was acquired there).
///
/// Symbols with a net quantity of zero are omitted from the result.
/// </summary>
public static class PositionCalculator
{
    public static IReadOnlyList<Position> Derive(IEnumerable<Trade> trades)
    {
        var positions = new List<Position>();

        // Group by symbol, then replay each symbol's trades in chronological order.
        var bySymbol = trades
            .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in bySymbol)
        {
            decimal net = 0m;
            decimal avg = 0m;

            foreach (var trade in group.OrderBy(t => t.Timestamp))
            {
                var signed = trade.SignedQuantity;
                var price = trade.Price;

                if (net == 0m)
                {
                    // Rule 1: opening from flat.
                    net = signed;
                    avg = price;
                }
                else if (Math.Sign(net) == Math.Sign(signed))
                {
                    // Rule 2: increasing exposure — weighted-average the new fill in.
                    var absNet = Math.Abs(net);
                    var absSigned = Math.Abs(signed);
                    avg = (avg * absNet + price * absSigned) / (absNet + absSigned);
                    net += signed;
                }
                else if (Math.Abs(signed) <= Math.Abs(net))
                {
                    // Rule 3: reducing exposure without crossing zero — avg unchanged.
                    net += signed;
                    if (net == 0m)
                    {
                        avg = 0m;
                    }
                }
                else
                {
                    // Rule 4: flipping through zero — reset avg to this trade's price.
                    net += signed;
                    avg = price;
                }
            }

            // Rule: omit flat symbols from the response.
            if (net != 0m)
            {
                positions.Add(new Position(group.Key, net, avg));
            }
        }

        return positions;
    }
}
