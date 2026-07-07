# Changelog

Notable changes to the GoldenTree Trade Blotter. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/); dates are UTC.

## [0.2.0] — 2026-07-07 — "Trading Desk" UI redesign

### Changed
- **Reworked the frontend layout** into a "Trading Desk" design (presentation-only;
  API, data model, Pinia store, and all `data-testid`s unchanged):
  - Trade entry moved into a horizontal **order-ticket command bar** at the top
    (Symbol · Buy/Sell toggle · Quantity · Price · Book Trade).
  - Added a **KPI summary strip** — Open Positions, Trades, Gross Exposure, Net Bias.
  - **Blotter** rows gained green/red **side-stripes** + color-coded pills; notional
    highlighted in gold; monospaced tabular numerics.
  - **Positions** rendered as **cards** with a LONG/SHORT chip, Net Qty, Avg Cost,
    Cost Basis, and an **exposure bar**.
- **New brand emblem** — a gold tile whose stacked canopy tiers double as an
  ascending chart (tree = growth = uptrend).
- Committed **dark trading-terminal palette**: gold + forest-green accents,
  green-biased dark neutrals, buy/sell semantics.

### Verified
- Frontend build clean; **Vitest 6/6**, **Playwright 5/5** (local & live).
- E2E readiness selector switched from header text to the `symbol-input` test id.

## [0.1.0] — 2026-07-07 — Initial full-stack build

### Added
- **Backend (C# / .NET 10)** — minimal API with `POST /trades`, `GET /trades`
  (newest first), `GET /positions` (derived). `PositionCalculator` implements
  weighted-average cost with **reset-on-flip** for mixed buys/sells. EF Core +
  SQLite (trades only). RFC 7807 validation → 400; enum-as-string JSON.
- **Seed data** — 28 demo trades across banks (WFC, BAC, JPM) and tech (GOOGL,
  MSFT, AAPL, NVDA), incl. a long→short flip (BAC) and a net-zero close (WFC).
- **Frontend (Vue 3 + Vite + Pinia)** — single `useBlotterStore`, trade-entry
  form, live blotter, reactive positions panel.
- **Tests** — 65 backend (position logic, per-endpoint, validation/error handling,
  `E2E/` availability + GET/POST), 6 Vitest form-validation, 5 Playwright E2E.
- **Docs** — README, architecture, business-requirements, cloud-session, test-case
  catalog + traceability matrix; GoldenTree-inspired brand assets; GitHub Pages
  landing page (`index.html`).
- **Deployment** — Dockerfiles + Railway (two Dockerfile-built services).

### Fixed
- SQLite cannot `ORDER BY DateTimeOffset` → sort newest-first in memory.
- `Enum.TryParse` accepted numeric `side` strings → guarded with `Enum.IsDefined`.
- Malformed request bodies returned 500 → now 400 via the exception handler.
- `import.meta.env` typing for the production build → added `src/vite-env.d.ts`.
- Local port mismatch (dev proxy 5000 vs `launchSettings` 5170) → pinned to 5000.
- Vue `v-model` on `<input type="number">` yields a number → defensive numeric
  validation in the form.
