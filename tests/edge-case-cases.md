# Edge-Case Test Cases (API Integration)

**Layer:** Edge · **Tooling:** xUnit + `WebApplicationFactory` (`backend/TradeBlotter.Tests`) against a
temp / in-memory SQLite DB · **Run:** `dotnet test`

These `E-##` cases exercise the boundary and critical behaviors **end-to-end over the real HTTP path**
(serialization, validation, EF Core persistence, and the derived-positions read). Each case gives concrete
JSON request bodies and the exact expected status codes and response assertions. JSON is camelCase; the
`side` enum serializes as a string. Money math uses exact `decimal`.

**Shared precondition for every case:** the app is started via `WebApplicationFactory` with a **clean,
isolated SQLite DB** (empty `Trade` table) unless the steps state otherwise. `notional = quantity × price`.

See the table legend in [`README.md §3`](./README.md#3-legend-for-the-test-case-tables).

---

## Positive / boundary behaviors

| ID | Title | Preconditions | Input / Steps (HTTP) | Expected Result | Requirement ref |
|---|---|---|---|---|---|
| **E-01** | Naked short creates a negative position | Empty DB | 1. `POST /trades` `{ "symbol": "aapl", "side": "Sell", "quantity": 100, "price": 25 }` → capture response. 2. `GET /positions`. | POST → **201**; body has server `id`, UTC `timestamp`, `symbol = "AAPL"` (upper-cased), `notional = 2500`. `GET /positions` → **200** with one entry: `{ symbol: "AAPL", netQuantity: -100, averageCost: 25 }` (negative / short; opened from flat at trade price, Rule 1). | BR §2,§4,§7 · ARCH edge #1 |
| **E-02** | Exact close-to-zero omitted from /positions, trades remain | Empty DB | 1. `POST /trades` `{ "symbol": "MSFT", "side": "Buy", "quantity": 100, "price": 10 }`. 2. `POST /trades` `{ "symbol": "MSFT", "side": "Sell", "quantity": 100, "price": 12 }`. 3. `GET /positions`. 4. `GET /trades`. | Both POSTs → **201**. `GET /positions` → **200** with **no `MSFT` entry** (net = 0, omitted). `GET /trades` → **200** still lists **both** trades (newest-first: the Sell, then the Buy), each with `notional` (`1200`, `1000`). Positions are derived, not stored; closing to zero does not delete trades. | BR §2,§3,§4 · ARCH edge #2 |
| **E-03** | Long→short flip resets avg cost (HTTP path) | Empty DB | Replay the canonical example via four `POST /trades` on `AAPL`: `Buy 100 @10`, `Buy 100 @12`, `Sell 50 @15`, `Sell 200 @20`. Then `GET /positions`. | All four POSTs → **201**. `GET /positions` → **200** with `{ symbol: "AAPL", netQuantity: -50, averageCost: 20 }` — the `Sell 200 @20` flips long→short and **resets** avg to the flip price (Rule 4). Confirms the worked example over HTTP (mirrors unit `U-03`). | Rule 4 · BR §3 · ARCH edge #3 |
| **E-04** | Fractional quantity & price preserve decimal precision | Empty DB | 1. `POST /trades` `{ "symbol": "BRKB", "side": "Buy", "quantity": 0.5, "price": 10.125 }` → capture body. 2. `GET /positions`. | POST → **201** with `notional = 5.0625` (`0.5 × 10.125`, exact — no float rounding error). `GET /positions` → **200** with `{ symbol: "BRKB", netQuantity: 0.5, averageCost: 10.125 }`. Decimal precision preserved through persistence and derivation. | BR §4 · ARCH edge #4 |

---

## Invalid submissions — each returns 400 problem-details and persists nothing

Common assertions for `E-05`–`E-08`: response is **400** with an RFC 7807 `application/problem+json` body
(`type`/`title`/`status`/`errors`); and a follow-up `GET /trades` shows the trade was **not persisted**
(count unchanged from before the bad POST).

| ID | Title | Preconditions | Input / Steps (HTTP) | Expected Result | Requirement ref |
|---|---|---|---|---|---|
| **E-05** | Empty symbol rejected | Empty DB | `POST /trades` `{ "symbol": "", "side": "Buy", "quantity": 100, "price": 10 }`, then `GET /trades`. | **400** problem-details citing `symbol`. `GET /trades` → **200** with **0** trades (nothing persisted). | BR §4 · ARCH §API design · edge #5 |
| **E-06** | Non-positive quantity rejected | Empty DB | `POST /trades` `{ "symbol": "AAPL", "side": "Buy", "quantity": 0, "price": 10 }`; repeat with `"quantity": -5`; then `GET /trades`. | Both → **400** problem-details citing `quantity` (rule `quantity > 0`). `GET /trades` → **200** with **0** trades. | BR §4 · ARCH §API design · edge #5 |
| **E-07** | Non-positive price rejected | Empty DB | `POST /trades` `{ "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 0 }`; repeat with `"price": -1`; then `GET /trades`. | Both → **400** problem-details citing `price` (rule `price > 0`). `GET /trades` → **200** with **0** trades. | BR §4 · ARCH §API design · edge #5 |
| **E-08** | Unknown side value rejected | Empty DB | `POST /trades` `{ "symbol": "AAPL", "side": "Hold", "quantity": 100, "price": 10 }`, then `GET /trades`. | **400** problem-details citing `side` (must be `Buy` or `Sell`). `GET /trades` → **200** with **0** trades. | BR §4 · ARCH §API design · edge #5 |

### Notes

- `E-05`–`E-08` collectively realize ARCH edge case #5 ("Invalid submissions … → 400 with problem-details,
  no trade persisted"), split per field so each failure mode is independently assertable.
- The global exception handler mapping unexpected failures to **500** problem-details (BR §4) is a
  cross-cutting concern; it is not exercised by these input-validation cases (which are deterministic 400s).
