# Trade Blotter Application — Project Architecture

> Living architecture document for the GoldenTree Trade Blotter exercise. Mirrors the approved implementation plan and is updated as the build progresses.

## Context

This is a full-stack take-home exercise (from `trade-blotter-exercise.docx`). We build a **trade blotter**: a user enters trades, sees them in a live table, and sees **positions automatically derived from trade history** (never stored). The graded dimensions are: domain modeling (correct average-cost on mixed buys/sells), API design (contracts, status codes, error handling), idiomatic Vue 3 Composition API + Pinia, UI scannability, tests on position logic, and repo/README quality. Guidance: **quality over completeness** — a polished working core beats a broad rough one.

### Confirmed decisions

- **Position logic:** weighted-average cost, **reset on sign flip** (long↔short); sells realize P&L and leave avg cost unchanged.
- **Frontend:** custom CSS, no component library — polished dark "trading terminal" look with color-coded Buy/Sell.
- **Scope:** tight, polished core (the spec) + comprehensive tests + strong README.
- **Persistence:** SQLite via EF Core.
- **Stack decision:** the exercise names .NET 8, but the machine has the **.NET 10.0.300 SDK** installed, so we target **`net10.0`** (C# 13) — no runtime install or roll-forward needed. This deviation from the spec's suggested version is noted in the README. Toolchain: .NET 10.0.300, Node v25.9, npm 11.12, git 2.50.

## Repository Structure

```
GoldenTree-TradeBlotter/
├── business-requirements.md        # living requirements doc (root)
├── project-architecture.md         # this file
├── cloud-session.md                # AI-assisted development session log
├── README.md                       # description, run + test instructions
├── .gitignore
├── backend/
│   ├── TradeBlotter.slnx            # solution (references API + moved test project)
│   ├── Dockerfile                   # backend container image (.NET 10 multi-stage)
│   └── TradeBlotter.Api/            # net10.0 minimal API
│       ├── Program.cs               #   PORT binding, configurable CORS, seed-on-boot
│       ├── Domain/                  #   Trade, Side, Position, PositionCalculator
│       ├── Dtos/                    #   TradeRequest, TradeResponse, PositionResponse
│       ├── Data/                    #   BlotterDbContext (EF Core SQLite) + SeedData
│       └── Endpoints/               #   /trades, /positions handlers + validation
├── frontend/
│   ├── index.html
│   ├── Dockerfile                   # frontend container image (build + static serve)
│   ├── vite.config.ts               # Vite + Vitest config, '@' alias, fs.allow
│   ├── playwright.config.ts         # E2E config (auto-starts both servers)
│   └── src/
│       ├── main.ts, App.vue         #   App shell + brand header
│       ├── stores/blotter.ts        #   Pinia store (single source of truth)
│       ├── api/client.ts            #   fetch wrapper (env-based API base URL)
│       ├── types.ts
│       ├── styles/                  #   palette.css (brand) + main.css
│       └── components/              #   TradeEntryForm, BlotterTable, PositionsPanel
├── tests/                          # ALL tests + human-readable catalog
│   ├── backend/TradeBlotter.Tests/  #   xUnit: unit, per-endpoint, validation, E2E/
│   ├── frontend/unit/               #   Vitest form-validation specs
│   ├── frontend/e2e/                #   Playwright end-to-end specs
│   ├── frontend/node_modules        #   symlink -> ../../frontend/node_modules (dep bridge)
│   └── *.md                         #   catalog (unit/edge/e2e) + traceability matrix
└── brand-assets/                   # GoldenTree-inspired palette, logos, favicon, icons
```

> **Test layout note:** the runnable tests were relocated under `tests/` with `backend/` and `frontend/` subfolders. The .NET solution (`backend/TradeBlotter.slnx`) references the moved xUnit project by relative path, so `dotnet test` from `backend/` still works. The frontend specs live outside the Vite project, so `tests/frontend/node_modules` is a symlink to `frontend/node_modules` (bridges bare-import resolution), and `vite.config.ts` sets `server.fs.allow: ['..']` plus an `@` alias so specs import app code as `@/…`.

## Architecture Constraints (graded qualities)

1. **API design** — clear endpoint contracts (documented request/response shapes), sensible error handling (validation → 400 with `ProblemDetails`; unexpected → 500 problem-details via global handler), appropriate HTTP status codes (201 on create, 200 on reads, 400 on bad input). JSON enums as strings; consistent camelCase.
2. **Vue patterns** — Composition API used naturally (`<script setup>`, `ref`/`computed`/`watch`), reactivity handled cleanly (derived state via `computed`, no manual DOM), **all shared state in the Pinia store, not scattered** across components. Components stay presentational; the store owns fetching and mutations.
3. **UI judgment** — a deliberate **"Trading Desk" layout** in a single committed dark "trading terminal" world: a horizontal **order-ticket command bar** for entry, a **KPI summary strip** (Open Positions, Trades, Gross Exposure, Net Bias) surfacing the summary before the detail, and a two-column body with a dominant **side-striped blotter** (color-coded Buy/Sell pills, monospaced tabular-nums, sticky sortable headers, notional in gold) beside a **positions rail of cards** (LONG/SHORT chip + exposure bar). The order ticket keeps entry intuitive (segmented Buy/Sell toggle, inline errors, disabled-while-submitting, clears on success). Small decisions compound into polish.

## Evaluation-Criteria Coverage (traceability)

| Required behavior | Where it's handled in this design |
|---|---|
| Positions **derived** from trades, not stored | `PositionCalculator.Derive()` recomputes on every `GET /positions`; only `Trade` rows persist in SQLite. |
| Net position of zero → symbol **omitted** | Output filter `netQty != 0`; unit test #4. |
| **Average-cost on mixed buys/sells** (critical) | 4-rule algorithm (open / increase-weighted-avg / reduce-unchanged / flip-reset); unit tests #1–3, #5. |
| Show net position **and average cost per symbol** | `Position { Symbol, NetQuantity, AverageCost }` → `PositionsPanel.vue`. |
| **Updates reactively** on new trade | Pinia store re-fetches trades + positions after a successful `POST`; both panels are `computed`/reactive off store state — no reload. |
| **Pinia** for state, **Vite** for tooling | `useBlotterStore` is the single source of truth; Vite dev server + build. (Custom CSS, no component lib — allowed by spec.) |
| **Unit tests double-checking position logic** | xUnit `PositionCalculatorTests` (8 pure cases) in `tests/backend/TradeBlotter.Tests/`, backed by per-endpoint, validation and `E2E/` suites — **65 backend tests** in total (see Testing Strategy), run via `dotnet test`. |

## Backend (C# / .NET 10, minimal API + EF Core SQLite)

### Domain types

- `enum Side { Buy, Sell }`
- `Trade` — `Id (Guid)`, `Symbol (string, upper-cased)`, `Side`, `Quantity (decimal)`, `Price (decimal)`, `Timestamp (DateTimeOffset, UTC, server-assigned)`.
- `Position` (computed, not persisted) — `Symbol`, `NetQuantity`, `AverageCost`.

### Position algorithm — `PositionCalculator.Derive(IEnumerable<Trade>)`

Pure, static, deterministic, unit-tested. This is the critical, graded piece. **Positions are always computed here from the trade list — never stored.** Group by symbol, process each symbol's trades **oldest→newest**, tracking signed `netQty` (Buy = +qty, Sell = −qty) and `avgCost`. Let `signedQty` be the incoming trade's signed quantity:

1. **Opening from flat** (`netQty == 0`): `netQty = signedQty`; `avgCost = price`.
2. **Increasing exposure** (`netQty` and `signedQty` same sign): weighted-average in the new fill —
   `avgCost = (avgCost*|netQty| + price*|signedQty|) / (|netQty| + |signedQty|)`; `netQty += signedQty`.
3. **Reducing exposure, no cross** (opposite signs, `|signedQty| <= |netQty|`): `netQty += signedQty`; **`avgCost` unchanged** (the closed portion realizes P&L, which the position's avg cost does not reflect). If `netQty` becomes exactly 0, the symbol is now flat.
4. **Flip through zero** (opposite signs, `|signedQty| > |netQty|`): the trade fully closes the old side and opens a new one; `netQty += signedQty` (now opposite sign); **`avgCost = price`** (reset to the flipping trade's price — the residual lot was acquired at this trade's price).

Output: one `Position { Symbol, NetQuantity, AverageCost }` per symbol **where `netQty != 0`** (zero-net symbols omitted). Use `decimal` throughout for exact money math; round only for display.

**Worked example (headline test):** Buy 100 @10, Buy 100 @12 → net **200 @11**; Sell 50 @15 → net **150 @11** (avg unchanged, rule 3); Sell 200 @20 → flip to **short 50 @20** (rule 4).

### Endpoints (minimal API)

| Method | Path | Request | Success | Errors |
|---|---|---|---|---|
| `POST` | `/trades` | `{ symbol, side, quantity, price }` | **201 Created** + created `TradeResponse` (server sets Id + UTC timestamp) | **400** problem-details on validation failure |
| `GET` | `/trades` | — | **200** all trades **newest first**, each with `notional = quantity*price` | — |
| `GET` | `/positions` | — | **200** derived positions per symbol (net qty, avg cost), zero-net omitted | — |

Validation: non-empty symbol, `side ∈ {Buy,Sell}`, `quantity>0`, `price>0`. Cross-cutting: CORS for the Vite dev origin; JSON enum-as-string; global exception handler → 500 problem-details.

### Persistence

EF Core + `Microsoft.EntityFrameworkCore.Sqlite`, `blotter.db` file, `EnsureCreated()` (or a migration) at startup. Only **trades** are stored; positions are always derived on read.

## Frontend (Vue 3 Composition API, Pinia, Vite, custom CSS)

### Explicit acceptance criteria (from the spec)

- **Trade entry form** (`TradeEntryForm.vue`)
  - Fields: **symbol, side (Buy/Sell), quantity, price**.
  - Basic validation: **no empty fields; quantity and price must be positive** (mirrors server validation).
  - On submit, the blotter **updates immediately without a page reload**.
- **Blotter table** (`BlotterTable.vue`)
  - Displays **all trades, newest first**.
  - Columns: **timestamp, symbol, side, quantity, price, notional value** (notional = quantity × price).
  - **Sortable by at least one column** (timestamp + notional at minimum).
  - **Scannable at a glance** — Buy/Sell color-coded (green/red pills), right-aligned monospaced numerics.
- **Positions panel** (`PositionsPanel.vue`)
  - Shows **current net position and average cost per symbol**.
  - **Updates reactively when a new trade is submitted** (driven off Pinia store state).

### State & components

- **Pinia store `useBlotterStore`** — single source of truth: `trades`, `positions`, loading/error flags; actions `fetchAll()`, `submitTrade(payload)`. After a successful POST, re-fetch trades **and** positions so both panels update reactively with no page reload.
- **Components** (all `<script setup lang="ts">`), laid out as the **"Trading Desk"**:
  - `App.vue` — the shell: a brand header with the refined **gold emblem** (a stacked-canopy tile whose tiers double as an ascending chart — tree = growth = uptrend), the horizontal order ticket, a **KPI summary strip** (Open Positions, Trades, Gross Exposure, Net Bias) computed off store state, then the two-column blotter + positions grid.
  - `TradeEntryForm.vue` — rendered as a **horizontal order-ticket command bar** (symbol · segmented Buy/Sell toggle · quantity · price · Book Trade); client-side validation mirroring the server; inline errors; disabled submit while pending; clears on success.
  - `BlotterTable.vue` — columns as above; newest first; sortable via a `computed` with sticky headers; rows carry a green/red **side-stripe** (left border) plus color-coded Buy/Sell pills; monospaced tabular-nums with notional highlighted in gold.
  - `PositionsPanel.vue` — a **rail of position cards**, each with a LONG/SHORT chip, Net Qty, Avg Cost, Cost Basis, and a small **exposure bar**; reacts to store changes.
- **Styling:** custom CSS, a committed **single dark "trading terminal" theme** — gold (#c9a227 / bright #e3c15a) and forest-green (#12563b) accents over green-biased dark neutrals (bg #0a0e0c, panel #10150f, surface #151b16, line #28322a), buy #2fb47c / sell #e5574e semantics; system sans for the brand, monospace + tabular-nums for all numerics/labels; two-column responsive layout (order ticket + KPI strip on top, blotter beside the positions rail).

## Testing Strategy

Tooling: **xUnit** (backend), **Vitest + Vue Test Utils** (frontend unit/component), **Playwright** (end-to-end). Current totals: **65 backend + 6 frontend-unit + 5 Playwright E2E = 76 tests, all passing (green).**

### Backend (xUnit) — `tests/backend/TradeBlotter.Tests/`

Split into focused files. The integration suites share an `ApiTestBase` + `BlotterApiFactory` (`WebApplicationFactory` over an **in-memory SQLite** connection, boot seeding **disabled for tests**) so each exercises the real HTTP pipeline against a clean database.

- **`PositionCalculatorTests`** — pure unit tests of the position/average-cost logic (no HTTP): long build-up weighted average; partial sell leaves avg cost **unchanged**; long→short flip through zero **resets** avg cost (headline case); close-to-zero symbol **omitted**; short build-up then partial cover (short-side symmetry); multi-symbol isolation; derivation ordered by timestamp regardless of input order; naked short opens a negative position.
- **Per-endpoint suites** — `PostTradesTests` (201 + `Location` header, computed notional, symbol upper-casing, case-insensitive `side`, server-assigned UTC timestamp, persistence, fractional precision), `GetTradesTests` (200, all trades returned, **newest-first** ordering, notional per row), `GetPositionsTests` (200, long / weighted-avg / partial-sell, net-zero omitted, multi-symbol independence, worked example over HTTP).
- **`ValidationAndErrorHandlingTests`** — every validation rule (non-empty symbol; `side ∈ {Buy,Sell}`; `quantity > 0`; `price > 0`; plus missing fields), malformed / wrong-type JSON body → **400**, `ProblemDetails` shape with field-level errors, and the guarantee that rejected trades are **never persisted**.
- **`E2E/`** — `ServerAvailabilityTests` (read endpoints reachable and 2xx, never 5xx / "service unavailable", serve JSON, respond within timeout, malformed body stays a 4xx) and `GetPostEndToEndTests` (GET/POST round trips: POST then GET a trade back, positions reflect posted trades, count increments by exactly one).

### Frontend unit (Vitest + Vue Test Utils) — `tests/frontend/unit/`

Form-validation specs on `TradeEntryForm.vue` (**U-F01…U-F06**): empty symbol blocks submit; zero/negative quantity blocked; zero/negative price blocked; empty/non-numeric quantity & price blocked; valid input calls `submitTrade` **once** with the normalized payload and resets the numeric fields; switching the Buy/Sell toggle is reflected in the submitted payload. Each asserts the store action fires (or doesn't) as expected.

### Frontend E2E (Playwright) — `tests/frontend/e2e/`

Five browser↔API scenarios (**T-01…T-05**); Playwright's `webServer` config **auto-starts both the .NET API and the Vue app**: book a valid trade → newest-first row + positions update with **no reload**; invalid trade → inline errors, no new row, no server write; buy-then-buy → correct net qty and weighted-average cost; click a sortable column header → blotter reorders; a sequence netting to zero → symbol **disappears** from positions but stays in the blotter.

**Bugs found & fixed during verification:** (1) SQLite cannot `ORDER BY` a `DateTimeOffset`, so `GET /trades` now sorts newest-first **in memory**; (2) `Enum.TryParse` accepted numeric strings for `side` (e.g. `"123"`) — now guarded with `Enum.IsDefined`; and (3) malformed request bodies now map to **400** (not 500) via the exception handler.

Runnable from the README: `dotnet test` (backend), `npm run test:unit` (frontend unit), `npm run test:e2e` (Playwright).

## Deployment (Railway — two services)

The app deploys to Railway as **two separate services** under the project **GoldenTree-TradeBlotter**:

- **backend** — built from `backend/Dockerfile` (.NET 10 SDK → ASP.NET runtime, multi-stage). Binds Kestrel to `0.0.0.0:$PORT` (Railway-injected). CORS is configurable via the `AllowedOrigins` env var (comma-separated, or `*` for the public demo). SQLite is **ephemeral** and re-seeded on each boot (no volume).
- **frontend** — built from `frontend/Dockerfile` (Node build → static `serve`). The Vue app calls the backend cross-origin using `VITE_API_URL`, baked in at **build time** (Vite inlines `import.meta.env.VITE_*`). In dev this var is unset, so the client uses relative paths through the Vite proxy.

Wiring order: deploy backend → generate its public domain → set the frontend's `VITE_API_URL` to that domain → deploy frontend → generate its domain → (CORS already permissive via `AllowedOrigins=*`). Production changes are backwards-compatible with local dev (relative URLs + dev CORS origins remain the default).

The **"Trading Desk" UI redesign** is deployed to the existing Railway **frontend** service at the same URL (presentation-only; the API, contracts, and `VITE_API_URL` wiring are unchanged).

## Verification (end-to-end)

1. `dotnet test` in `backend/` → all **65 backend tests** (position-logic unit + per-endpoint, validation and `E2E/` suites) pass.
2. `npm run test:unit` → the **6 form-validation specs** (U-F01…U-F06) pass.
3. `npm run test:e2e` → the **5 Playwright** front-to-back scenarios (T-01…T-05) pass against the running app.
4. Manual smoke: `dotnet run` backend + `npm run dev` frontend; submit a trade → blotter row appears newest-first and positions update **without reload**; toggle a column sort; confirm Buy/Sell color coding. Verify the worked-example sequence (…Sell 200@20) yields short 50 @20 in the positions panel and via `GET /positions`.

## Open Items / Risks

- **.NET version (resolved):** targeting **`net10.0`** to match the installed SDK (spec suggested .NET 8). No roll-forward or extra runtime install needed; documented in the README.
- **Git:** repo not yet initialized. Final submission needs `git init`, a public GitHub repo, and the Claude Code transcript included — done at the end, only when the user asks to push.
