# AGENTS.md

Guidance for AI coding agents working in this repository. Human-facing docs:
[README.md](README.md) (setup + usage), [project-architecture.md](project-architecture.md)
(design), [business-requirements.md](business-requirements.md) (requirements),
[CHANGELOG.md](CHANGELOG.md) (notable changes).

## What this is

A full-stack **trade blotter**: enter trades, see them in a live table, and see
**positions derived from trade history** (never stored). Backend **C# / .NET 10**
(minimal API + EF Core SQLite); frontend **Vue 3** (Composition API, Pinia, Vite,
TypeScript). UI is the dark "Trading Desk" layout (order-ticket bar, KPI strip,
side-striped blotter, position cards).

## Repository map

- `backend/TradeBlotter.Api/` — the API. `Domain/` (Trade, Side, Position,
  **PositionCalculator** — the critical average-cost logic), `Dtos/`, `Data/`
  (`BlotterDbContext`, `SeedData`), `Endpoints/`, `Program.cs`.
- `frontend/src/` — `stores/blotter.ts` (Pinia, single source of truth),
  `api/client.ts`, `components/` (TradeEntryForm, BlotterTable, PositionsPanel),
  `styles/`.
- `tests/backend/TradeBlotter.Tests/` — xUnit (unit, per-endpoint, validation, `E2E/`).
- `tests/frontend/unit/` — Vitest; `tests/frontend/e2e/` — Playwright.
- `tests/*.md` — human-readable test-case catalog + traceability matrix.

## Build / run / test

| Task | Command |
|---|---|
| Run backend | `cd backend/TradeBlotter.Api && dotnet run` → http://localhost:5000 |
| Run frontend | `cd frontend && npm install && npm run dev` → http://localhost:5173 |
| Backend tests (65) | `cd backend && dotnet test` |
| Frontend unit (6) | `cd frontend && npm run test:unit` |
| E2E (5) | `cd frontend && npm run test:e2e` (auto-starts both servers) |
| E2E vs live | `cd frontend && PLAYWRIGHT_BASE_URL=<url> npx playwright test` |
| Frontend build | `cd frontend && npm run build` |

The frontend dev server **proxies** `/trades` and `/positions` to the backend on
port 5000 — start the backend first. Both ports (5000 / 5173) must match; the
backend port is pinned in `Properties/launchSettings.json`.

## Conventions & invariants (do not break)

- **Positions are derived, never persisted.** Only `Trade` rows are stored;
  `PositionCalculator.Derive()` recomputes on every `GET /positions`. Keep money
  math in `decimal`. Net-zero symbols are omitted from the response.
- **Average-cost rules** (replay oldest→newest, signed net qty): open-from-flat →
  price; increase → weighted avg; reduce (no cross) → avg unchanged; flip through
  zero → avg resets to the flip price. Any change here needs matching tests in
  `PositionCalculatorTests`.
- **All shared state in the Pinia store**, not scattered in components. Components
  stay presentational; the store owns fetching/mutations and refreshes trades +
  positions after a submit.
- **Preserve `data-testid`s** — the Vitest/Playwright suites rely on them:
  `symbol-input`, `side-buy`, `side-sell`, `quantity-input`, `price-input`,
  `submit-trade`, `submit-flash`, `*-error`, `trade-row`, `trade-symbol`,
  `sort-timestamp`, `sort-notional`, `position-row` (+ `data-symbol`),
  `position-qty`, `position-avg`, `kpi-positions`, `kpi-trades`, `global-error`.
- **API contract:** `POST /trades` → 201 (+ created trade w/ notional); validation
  or malformed body → **400** RFC 7807 problem-details; `GET /trades` (newest
  first) / `GET /positions` → 200. Enum-as-string, camelCase JSON.
- **Always run the relevant suite after a change** and confirm it passes before
  claiming done. UI edits: keep test IDs and re-run Playwright.

## Gotchas

- SQLite cannot `ORDER BY DateTimeOffset` → `GET /trades` sorts newest-first in
  memory. Decimals are stored as TEXT to preserve precision.
- `Enum.TryParse` accepts numeric strings for `side`; validation guards with
  `Enum.IsDefined`.
- `tests/frontend/` sits outside the Vite project; `tests/frontend/node_modules`
  is a symlink to `frontend/node_modules` (committed via a trailing-slash
  `.gitignore` rule), and `vite.config.ts` sets `server.fs.allow: ['..']` + an
  `@` → `src` alias.

## Deployment (Railway)

Two Dockerfile-built services in project **GoldenTree-TradeBlotter**. Deploy each
from the repo root with `railway up <dir> --path-as-root --service <name>`
(the sub-folder Dockerfile must be at the archive root). Service Builder must be
**Dockerfile** (Railpack otherwise serves a static Caddy site). Frontend bakes
`VITE_API_URL` at build time; backend reads `PORT` and `AllowedOrigins`.
