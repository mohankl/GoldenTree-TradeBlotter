# Trade Blotter — Business Requirements

> **Living document.** Captures what we're building and why, derived from `trade-blotter-exercise.docx`. Updated as the project progresses. Companion to `project-architecture.md` (the *how*).

**Status:** Planning complete → implementation starting.
**Last updated:** 2026-07-07.

---

## 1. Overview & Goals

Build a **trade blotter** — the kind of tool traders use daily. A user enters trades, views them in a live blotter table, and sees their **current positions automatically derived from the trade history**. The evaluation focuses on how we model the domain, design the UI, and wire the full stack together.

**Primary goals**
- Enter trades and see them immediately in a live table (no page reload).
- Derive positions (net quantity, average cost) per symbol from trade history — never stored separately.
- Correct **average-cost** behavior on **mixed buys/sells** (the critical, explicitly-graded criterion).
- Clean domain types, clear API contracts, idiomatic Vue 3 + Pinia, a scannable UI, and tests on the position logic.

**Guiding principle:** *Quality over completeness* — a polished, working core beats a feature-complete but rough submission.

## 2. Domain Model

| Concept | Fields | Notes |
|---|---|---|
| **Trade** | `id`, `symbol`, `side` (Buy/Sell), `quantity` (shares), `price` (per share), `timestamp` | Immutable record of an execution. Server assigns `id` and `timestamp` (UTC). |
| **Side** | `Buy` \| `Sell` | Buy increases exposure (+qty); Sell decreases (−qty). |
| **Position** (derived) | `symbol`, `netQuantity`, `averageCost` | Computed from trades on every read; **never persisted**. Zero-net symbols omitted. |

## 3. Position / Average-Cost Rules (critical)

Positions are derived by replaying a symbol's trades oldest→newest, tracking signed net quantity and average cost:

1. **Open from flat** → avg cost = trade price.
2. **Increase exposure** (same direction) → weighted-average the new fill into avg cost:
   `avg = (avg·|net| + price·|qty|) / (|net| + |qty|)`.
3. **Reduce exposure** (opposite direction, not crossing zero) → net moves toward zero; **avg cost unchanged** (the closed portion realizes P&L, which the position doesn't reflect).
4. **Flip through zero** (opposite direction, larger than current net) → old side fully closed, remainder opens the other side; **avg cost resets** to the flipping trade's price.

**Worked example:** Buy 100 @10, Buy 100 @12 → **200 @11**; Sell 50 @15 → **150 @11** (avg unchanged); Sell 200 @20 → **short 50 @20** (flip resets avg).

*Design decision:* we model avg cost of the **open** position only; realized/unrealized P&L is out of scope for the initial build (noted as a future enhancement).

## 4. API Requirements (backend)

REST API. All money math uses exact decimals; enums serialized as strings; camelCase JSON.

| Method | Path | Purpose | Success | Validation / errors |
|---|---|---|---|---|
| `POST` | `/trades` | Submit a new trade | **201** + created trade | `symbol` non-empty; `side ∈ {Buy,Sell}`; `quantity > 0`; `price > 0`; else **400** problem-details |
| `GET` | `/trades` | All trades, **newest first** | **200** (each includes `notional = quantity × price`) | — |
| `GET` | `/positions` | Derived positions per symbol | **200** (net qty + avg cost; zero-net omitted) | — |

Unexpected failures → **500** problem-details via a global handler.

## 5. Frontend Requirements

**Trade entry form** — fields symbol, side (Buy/Sell), quantity, price; basic validation (no empty fields; quantity & price positive); on submit the blotter updates **immediately without a page reload**.

**Blotter table** — all trades, newest first; columns timestamp, symbol, side, quantity, price, **notional**; sortable by at least one column; **scannable at a glance** with Buy/Sell shown visually (color-coded).

**Positions panel** — current net position and average cost per symbol; **updates reactively** when a new trade is submitted.

**Technical:** Vue 3 Composition API, **Pinia** for state (single source of truth, not scattered), **Vite** for tooling. Custom CSS (no component library).

## 6. Non-Functional Requirements

- **Persistence:** SQLite (via EF Core). Only trades stored; positions derived on read.
- **Reactivity:** submitting a trade refreshes both the blotter and positions without reload.
- **Tests:** unit tests double-checking the position logic (critical), plus form-validation, edge-case, and end-to-end coverage. See `project-architecture.md` → Testing Strategy.
- **Repo quality:** readable README with setup instructions and clear commit history.

## 7. Assumptions & Design Decisions

- **.NET 10** is used instead of the spec's .NET 8 (matches the installed SDK); documented in the README.
- **Naked shorts allowed:** a Sell with no prior position creates a negative net position (no short-selling restriction stated).
- **Symbols normalized** to uppercase (e.g. `aapl` → `AAPL`).
- **Average cost** reflects the open position only (no realized P&L line in v1).
- **Timestamps** are server-assigned UTC; the client displays local/formatted time.

## 8. Out of Scope (per exercise)

- Real market-data integration.
- Authentication / authorization.
- Multi-user concurrency, order routing, settlement.

## 9. Open Questions / Progress Log

- **2026-07-07** — Requirements analyzed from docx; plan approved. Confirmed: avg-cost with flip-reset, custom-CSS UI, SQLite, .NET 10, four-layer test strategy. Docs (`business-requirements.md`, `project-architecture.md`) created. Next: scaffold backend solution.
