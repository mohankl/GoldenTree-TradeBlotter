# Unit Test Cases

**Layer:** Unit · **Tooling:** xUnit (`backend/TradeBlotter.Tests`) for position logic; Vitest + Vue Test Utils
(`frontend/src/components/__tests__`) for form validation · **Run:** `dotnet test` / `npm run test:unit`

These cases isolate the two purest, most critical pieces of logic:

1. **`U-##`** — `PositionCalculator.Derive(IEnumerable<Trade>)`, the pure/static/deterministic position engine
   (the explicitly graded piece). Each case gives an exact trade sequence and the **exact** expected
   `netQuantity` and `averageCost`. Sign convention: **Buy = +qty, Sell = −qty**; symbol trades are replayed
   **oldest → newest**. Rule numbers refer to the four rules in
   [`README.md §1`](./README.md) / `business-requirements.md §3`.
2. **`U-F##`** — client-side validation in `TradeEntryForm.vue`, mirroring the server rules.

See the table legend in [`README.md §3`](./README.md#3-legend-for-the-test-case-tables).

---

## Part A — Position-calculation unit cases (`U-##`)

| ID | Title | Preconditions | Input / Steps (trade sequence, oldest→newest) | Expected Result | Requirement ref |
|---|---|---|---|---|---|
| **U-01** | Long build-up → weighted average | Empty trade list | `Buy 100 @10`, `Buy 100 @12` (symbol `AAPL`) | 1 position: `AAPL` `netQuantity = +200`, `averageCost = 11` — `(10·100 + 12·100)/200 = 11` (Rule 2). Matches first step of the canonical example. | Rule 1,2 · BR §3 · ARCH unit #1 |
| **U-02** | Partial sell leaves avg cost unchanged | Empty trade list | `Buy 100 @10`, `Buy 100 @12`, `Sell 50 @15` (`AAPL`) | `AAPL` `netQuantity = +150`, `averageCost = 11` (**unchanged** — reduce exposure, no cross, Rule 3). Canonical example step 2. | Rule 3 · BR §3 · ARCH unit #2 |
| **U-03** | **Flip through zero → avg cost resets (headline)** | Empty trade list | `Buy 100 @10`, `Buy 100 @12`, `Sell 50 @15`, `Sell 200 @20` (`AAPL`) | `AAPL` `netQuantity = −50` (short), `averageCost = 20` — the `Sell 200` closes the +150 long and opens −50 short at the flip price (Rule 4). **This is the full canonical worked example.** | Rule 4 · BR §3 · ARCH unit #3 |
| **U-04** | Close to exactly zero → symbol omitted | Empty trade list | `Buy 100 @10`, `Sell 100 @12` (`AAPL`) | `netQuantity` hits exactly `0` → **no `AAPL` entry** in the derived positions (output list empty). Avg cost is unchanged on the reducing sell (Rule 3) but the flat symbol is dropped. | Rule 3 · BR §2,§3 · ARCH unit #4 |
| **U-05** | Short build-up then partial cover | Empty trade list | `Sell 100 @50`, `Sell 100 @40`, `Buy 50 @30` (`TSLA`) | After two sells: `netQuantity = −200`, `averageCost = 45` — `(50·100 + 40·100)/200 = 45` (short-side Rule 2). `Buy 50` covers part of the short (reduce, no cross, Rule 3): `netQuantity = −150`, `averageCost = 45` (**unchanged**). | Rule 2,3 · BR §3 · ARCH unit #5 |
| **U-06** | Multi-symbol isolation | Empty trade list | `AAPL Buy 100 @10`, `MSFT Sell 10 @300`, `AAPL Buy 100 @12` | 2 independent positions: `AAPL` `netQuantity = +200`, `averageCost = 11`; `MSFT` `netQuantity = −10`, `averageCost = 300`. Neither symbol's trades affect the other. | BR §2 · ARCH unit #6 |
| **U-07** | Ordering independent of input order (sorts by timestamp) | Empty trade list | Same four trades as U-03 with timestamps `t1<t2<t3<t4` assigned as: `t1 Buy 100 @10`, `t2 Buy 100 @12`, `t3 Sell 50 @15`, `t4 Sell 200 @20`, but **supplied to `Derive` in scrambled order** (e.g. `t4, t1, t3, t2`) | Identical to U-03: `AAPL` `netQuantity = −50`, `averageCost = 20`. Derivation replays each symbol strictly oldest→newest by `timestamp`, so input ordering is irrelevant. | BR §3 · ARCH unit #7 |
| **U-08** | Naked short opens a negative position | Empty trade list | `Sell 100 @25` (`GOOG`, no prior position) | `GOOG` `netQuantity = −100` (short), `averageCost = 25` — opening from flat sets avg = trade price (Rule 1). Naked shorts are allowed (no short-selling restriction). | Rule 1 · BR §2,§7 · ARCH unit #1 |
| **U-09** | Increase short exposure → weighted average (short-side symmetry) | Empty trade list | `Sell 100 @50`, `Sell 300 @70` (`NVDA`) | `NVDA` `netQuantity = −400`, `averageCost = 65` — `(50·100 + 70·300)/400 = 26000/400 = 65` (Rule 2 on the short side). | Rule 2 · BR §3 |
| **U-10** | Flip short → long → avg cost resets | Empty trade list | `Sell 100 @50`, `Buy 300 @60` (`META`) | `Sell 100` opens short: `−100 @50`. `Buy 300` (`|300| > |100|`) closes the short and opens long (Rule 4): `netQuantity = +200`, `averageCost = 60`. Mirror of U-03 in the opposite direction. | Rule 4 · BR §3 |

### Derived-behavior notes

- **Exact decimals:** `averageCost` is computed with `decimal` arithmetic; assertions compare exact values
  (no floating-point tolerance needed for the integer-priced cases above). See `E-04` for the fractional-precision case.
- **Omission is the only "empty" signal:** a flat symbol produces **no** position entry — there is no
  zero-quantity row (U-04).
- U-01 → U-02 → U-03 form one continuous replay and together reproduce the canonical worked example
  in `business-requirements.md §3`.

---

## Part B — Frontend form-validation unit cases (`U-F##`)

Component under test: `TradeEntryForm.vue`. Client validation **mirrors** the server rules
(`symbol` non-empty; `quantity > 0`; `price > 0`). Assertions are on rendered inline errors, submit
blocking, and whether the Pinia store's `submitTrade` action is invoked (mocked/spied in the test).

| ID | Title | Preconditions | Input / Steps | Expected Result | Requirement ref |
|---|---|---|---|---|---|
| **U-F01** | Empty symbol blocks submit | Form mounted with a spy on the store `submitTrade` action | Leave `symbol` empty; set `side = Buy`, `quantity = 100`, `price = 10`; click **Submit** | Inline validation error shown on the `symbol` field; submit is **blocked**; `submitTrade` is **not** called (0 invocations); no new blotter row. | BR §5 · ARCH form-validation #1 |
| **U-F02** | Non-positive quantity blocks submit | Form mounted, store action spied | Set `symbol = AAPL`, `side = Buy`, `price = 10`; enter `quantity = 0`, then repeat with `quantity = -5`; click **Submit** each time | Inline error on `quantity` for **both** values (`0` and negative); submit blocked; `submitTrade` **not** called. | BR §5 · ARCH form-validation #2 |
| **U-F03** | Non-positive price blocks submit | Form mounted, store action spied | Set `symbol = AAPL`, `side = Buy`, `quantity = 100`; enter `price = 0`, then `price = -1`; click **Submit** each time | Inline error on `price` for both values; submit blocked; `submitTrade` **not** called. | BR §5 · ARCH form-validation #3 |
| **U-F04** | Non-numeric quantity / price blocks submit | Form mounted, store action spied | Set `symbol = AAPL`, `side = Buy`; enter `quantity = "abc"` (or empty), `price = "1.2.3"`; click **Submit** | Inline error(s) on the non-numeric field(s); submit blocked; `submitTrade` **not** called. | BR §5 · ARCH form-validation #4 |
| **U-F05** | Valid input submits once and resets | Form mounted, store action spied (resolves successfully) | Set `symbol = aapl`, `side = Buy`, `quantity = 100`, `price = 10`; click **Submit** | `submitTrade` called **exactly once** with payload `{ symbol: "aapl" (or "AAPL" if client upper-cases), side: "Buy", quantity: 100, price: 10 }`; no validation errors; the form **resets** (fields cleared) on success. No page reload. | BR §5 · ARCH form-validation #5 |
| **U-F06** | No double-submit while pending | Form mounted; store `submitTrade` returns a pending (unresolved) promise | Fill valid fields (`AAPL / Buy / 100 / 10`); click **Submit** twice in quick succession before the promise resolves | Submit control is **disabled while pending**; `submitTrade` is called **at most once**; no duplicate trade. | ARCH §Architecture Constraints (Vue patterns — disabled-while-submitting) |

### Notes on the form cases

- Symbol upper-casing is authoritative on the **server** (BR §7). The client MAY upper-case for display;
  `U-F05`'s payload assertion should match the component's actual behavior — assert `"AAPL"` if the
  component normalizes, otherwise `"aapl"` (the server will normalize on receipt — see `E-…` / T-cases).
- These are pure component tests: the store is mocked, so **no HTTP** is issued. Real front-to-back
  submission is covered by the Playwright cases (`T-01`, `T-02`).
