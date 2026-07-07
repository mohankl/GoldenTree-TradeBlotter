# Requirements Traceability Matrix

Maps each graded requirement (from [`../business-requirements.md`](../business-requirements.md) and
[`../project-architecture.md`](../project-architecture.md)) to the specific test-case IDs that cover it.
IDs are defined in [`unit-test-cases.md`](./unit-test-cases.md) (`U-##`, `U-F##`),
[`edge-case-cases.md`](./edge-case-cases.md) (`E-##`), and [`e2e-test-cases.md`](./e2e-test-cases.md) (`T-##`).

**Coverage legend:** ● covered at that layer · — not applicable at that layer.

---

## 1. Core graded requirements

| # | Requirement | Source | Unit | Edge (API) | E2E | Covering case IDs |
|---|---|---|---|---|---|---|
| R1 | **Positions derived from trade history, never stored** | BR §2,§3 · ARCH traceability | ● | ● | ● | `U-01`–`U-10`, `E-02`, `E-03`, `T-05` |
| R2 | **Net-zero symbols omitted** from positions | BR §2,§3 | ● | ● | ● | `U-04`, `E-02`, `T-05` |
| R3 | **Average-cost on mixed buys/sells** (open / weighted-avg increase / unchanged on reduce / **flip reset**) — the critical case | BR §3 · ARCH edge #3 | ● | ● | ● | `U-01`, `U-02`, `U-03` (headline), `U-05`, `U-09`, `U-10`, `E-03`, `T-03` |
| R3a | Flip-through-zero **resets** avg cost (canonical worked example) | BR §3 (Rule 4) | ● | ● | — | `U-03`, `U-07`, `U-10`, `E-03` |
| R3b | Reduce exposure leaves avg cost **unchanged** | BR §3 (Rule 3) | ● | — | ● | `U-02`, `U-05`, `T-03` |
| R4 | Show **net position and average cost per symbol** | BR §2,§5 | ● | ● | ● | `U-06`, `E-01`, `T-01`, `T-03` |
| R5 | **Updates reactively** on new trade, **no page reload** | BR §5 · ARCH E2E #1 | — | — | ● | `T-01`, `T-03`, `T-05` |
| R6 | **Pinia** single source of truth + **Vite** tooling | BR §5 · ARCH §Vue patterns | ● | — | ● | `U-F05` (store action invoked), `T-01`, `T-05` (reactive store-driven update) |

---

## 2. Frontend form & UI requirements

| # | Requirement | Source | Unit | Edge | E2E | Covering case IDs |
|---|---|---|---|---|---|---|
| R7 | **Form validation** mirrors server (no empty fields; qty & price positive; numeric) | BR §5 · ARCH form-validation | ● | — | ● | `U-F01`, `U-F02`, `U-F03`, `U-F04`, `T-02` |
| R7a | Valid input submits **once** and form **resets**; no double-submit | ARCH form-validation #5 | ● | — | ● | `U-F05`, `U-F06`, `T-01` |
| R8 | Blotter **columns** timestamp/symbol/side/quantity/price/notional | BR §5 | — | — | ● | `T-01`, `T-03` |
| R8a | Blotter **newest-first** ordering | BR §4,§5 | — | ● | ● | `E-02`, `T-01`, `T-03`, `T-05` |
| R8b | Blotter **sortable** by ≥1 column | BR §5 · ARCH E2E #4 | — | — | ● | `T-04` |
| R8c | **Scannable** — Buy/Sell color-coded | BR §5 · ARCH §UI judgment | — | — | ● | `T-06`, `T-03` |

---

## 3. API contract, status codes & error handling

| # | Requirement | Source | Unit | Edge | E2E | Covering case IDs |
|---|---|---|---|---|---|---|
| R9 | `POST /trades` valid → **201** + created trade | BR §4 · ARCH endpoints | — | ● | ● | `E-01`, `E-03`, `E-04`, `T-01` |
| R9a | `GET /trades` → **200**, newest-first, each with `notional` | BR §4 | — | ● | ● | `E-02`, `T-01`, `T-03` |
| R9b | `GET /positions` → **200**, net qty + avg cost, zero-net omitted | BR §4 | ● | ● | ● | `U-04`, `E-01`, `E-02`, `T-05` |
| R10 | Validation failures → **400** RFC 7807 problem-details, **nothing persisted** | BR §4 · ARCH §API design | ● (client) | ● | ● | `U-F01`–`U-F04`, `E-05`, `E-06`, `E-07`, `E-08`, `T-02` |
| R10a | `symbol` non-empty | BR §4 | ● | ● | ● | `U-F01`, `E-05`, `T-02` |
| R10b | `quantity > 0` | BR §4 | ● | ● | ● | `U-F02`, `E-06`, `T-02` |
| R10c | `price > 0` | BR §4 | ● | ● | — | `U-F03`, `E-07` |
| R10d | `side ∈ {Buy, Sell}` | BR §4 | — | ● | — | `E-08` |

---

## 4. Domain-fact requirements

| # | Requirement | Source | Unit | Edge | E2E | Covering case IDs |
|---|---|---|---|---|---|---|
| R11 | `notional = quantity × price` on responses | BR §4 | — | ● | ● | `E-01`, `E-04`, `T-01`, `T-05` |
| R12 | Server assigns `id` + UTC `timestamp` | BR §2,§4 | — | ● | ● | `E-01`, `T-01` |
| R13 | Symbol **normalized to uppercase** | BR §7 | — | ● | — | `E-01` (`aapl` → `AAPL`) |
| R14 | **Naked shorts allowed** (Sell with no prior position → negative net) | BR §7 · ARCH edge #1 | ● | ● | — | `U-08`, `E-01` |
| R15 | **Fractional** quantity & price preserve decimal precision | ARCH edge #4 | — | ● | — | `E-04` (`0.5 @ 10.125` → notional `5.0625`) |
| R16 | Derivation processes **oldest→newest by timestamp**, independent of input order | BR §3 · ARCH unit #7 | ● | — | — | `U-07` |
| R17 | **Multi-symbol isolation** | BR §2 · ARCH unit #6 | ● | — | ● | `U-06`, `T-04` |

---

## 5. Coverage summary

| Requirement group | Requirements | Any gaps? |
|---|---|---|
| Core position/derivation (R1–R6) | 8 (incl. R3a/R3b) | None — covered across all three layers |
| Frontend form & UI (R7–R8c) | 6 | None |
| API contract & errors (R9–R10d) | 9 | None |
| Domain facts (R11–R17) | 7 | None |

Every requirement traces to at least one executable test case. The **critical** average-cost logic (R3,
R3a, R3b) is intentionally covered redundantly at the unit (`U-01`–`U-03`, `U-05`, `U-09`, `U-10`), API
(`E-03`), and E2E (`T-03`) layers, with the **canonical worked example** anchored in `U-03` / `E-03`.
