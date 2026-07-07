# GoldenTree Trade Blotter — Brand Guidelines

A compact, institutional brand system for the **GoldenTree Trade Blotter** web
app: a dark, professional trading terminal with a refined gold accent, deep
forest-green secondary, and clear Buy/Sell signal colors — balanced for a
fintech / trade-blotter context.

---

## Source & inspiration

These assets are **original works** created for this project, visually inspired
by the refined, institutional aesthetic of **GoldenTree Asset Management**
(<https://www.goldentree.com/>). They are **not** the firm's actual trademarked
logo, wordmark, or brand files, and no copyrighted assets were downloaded or
embedded.

Research notes on the public site: it presents as a minimalist, corporate
asset-management brand — generous negative space, a clean sans-serif type
system, restrained dark neutrals, understated "seasoned expertise" tone, and
gold undertones implied by the name (with green implied by "Tree"). No explicit
hex values were exposed on the page at time of research, so the palette below is
a **sensible, original approximation** — refined gold + deep forest green + dark,
green-biased trading neutrals — chosen to match that aesthetic and to serve a
trading UI.

---

## Color palette

### Brand — gold

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-gold`        | `#C9A227` | **Primary brand gold.** Logo, primary buttons, key accents. |
| `--gt-gold-bright` | `#E3C15A` | Brighter gold for hovers, highlights, the "Tree" wordmark. |
| `--gt-gold-soft`   | `#EFD68A` | Soft gold — subtle fills, focus rings, top gradient stop. |
| `--gt-gold-deep`   | `#9A7B1C` | Deep gold — pressed states, the trunk, borders on gold. |

### Brand — forest green

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-green`        | `#12563B` | **Secondary brand** — deep forest green (tiles, brand chrome). |
| `--gt-green-mid`    | `#1B6B4A` | Mid green for secondary surfaces and tints. |
| `--gt-green-bright` | `#2FA06E` | Brighter green for links / emphasis on dark. |

### Semantic — Buy / Sell

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-buy`        | `#2FB47C` | **BUY** side — direction badges, positive P&L, `buy.svg`. |
| `--gt-buy-strong` | `#229A66` | BUY pressed / strong emphasis. |
| `--gt-buy-soft`   | `rgba(47,180,124,.14)` | BUY row / tag background tint. |
| `--gt-sell`       | `#E5574E` | **SELL** side — direction badges, negative P&L, `sell.svg`. |
| `--gt-sell-strong`| `#C93F37` | SELL pressed / strong emphasis. |
| `--gt-sell-soft`  | `rgba(229,87,78,.14)` | SELL row / tag background tint. |

> Keep Buy/Sell green distinct from the deep brand green. Brand green is
> **chrome**; `--gt-buy` is a **signal**. Never use brand green to mean "Buy".

### Status

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-info`    | `#3FB6D6` | Informational / neutral status. |
| `--gt-warning` | `#E0A62E` | Warning / pending fills. |

### Dark, green-biased neutrals (trading terminal)

| Token | Hex | Usage |
|-------|-----|-------|
| `--gt-bg`            | `#0A0E0C` | App background — near-black, green-biased. |
| `--gt-bg-elevated`   | `#0D1310` | Slightly raised background regions. |
| `--gt-surface`       | `#151B16` | Cards, panels, table body. |
| `--gt-surface-2`     | `#1E261F` | Elevated surface — headers, menus, hover. |
| `--gt-surface-3`     | `#28322A` | Highest surface — popovers, active rows. |
| `--gt-border`        | `#2A352C` | Default borders / dividers. |
| `--gt-border-strong` | `#3A473C` | Emphasized borders, table gridlines. |
| `--gt-text`          | `#E8EDE9` | Primary text. |
| `--gt-text-muted`    | `#9AA79C` | Secondary / muted text. |
| `--gt-text-faint`    | `#64716A` | Disabled / placeholder text. |
| `--gt-text-on-gold`  | `#0A0E0C` | Text on a gold fill (dark, for contrast). |
| `--gt-text-on-green` | `#E9F5EE` | Text on a deep-green fill. |

All tokens are defined in [`palette.css`](./palette.css). **Token names are
stable** — the app imports the file; only values are tuned.

---

## Typography

Use a **system font stack** — no web-font dependency, fast, and native to the
trading desk OS:

```css
font-family: 'Inter', 'Helvetica Neue', Helvetica, Arial, system-ui,
             -apple-system, 'Segoe UI', Roboto, sans-serif;
```

- **Wordmark / headings:** weight 700, tight letter-spacing (~0.2px).
- **Body / UI:** weight 400–500.
- **Numeric data (prices, quantities, P&L):** use a tabular / monospaced feel
  for column alignment, e.g. `font-variant-numeric: tabular-nums;` or a mono
  stack: `'SF Mono','JetBrains Mono',ui-monospace,Menlo,Consolas,monospace`.

The wordmark in `logo.svg` sets its text with this system stack via
`font-family` and therefore **depends on the host system fonts** (Inter if
present, otherwise the next available sans-serif). This keeps the SVG
self-contained with no embedded fonts. If pixel-perfect rendering across all
machines is required, convert the wordmark text to paths.

---

## The mark

The GoldenTree emblem is a **layered golden conifer** built from three stacked
canopy tiers over a short trunk. The tiers step wider as they descend and share
one vertical gold gradient (`#EFD68A → #E3C15A → #C9A227`), so the silhouette
reads two ways at once:

- a **tree** (the brand, growth, stability), and
- an **ascending chart / uptrend** (the trading context — a rising market).

Geometry is symmetric about the vertical axis with consistent tier steps and a
balanced trunk, so it stays crisp from 512px down to a 16px favicon.

### Logo usage

- **`logo.svg`** — horizontal wordmark (mark + "Golden" in light, "Tree" in
  gold). Use in the app header, login, and about panel. Built for **dark
  backgrounds**; viewBox `300×64`.
- **`logo-mark.svg`** — mark only, `64×64`. Use where the wordmark is too wide:
  collapsed sidebars, compact headers, loading/splash states.
- **`favicon.svg`** — bold, simplified two-tier mark on a forest-green tile;
  solid gold (no gradient) so it stays sharp at 16–32px.
- **`icon-192.svg` / `icon-512.svg`** — PWA / installable app icons: the mark on
  a branded forest-green tile with a subtle gold hairline border.

Guidelines:
- Maintain clear space around the logo equal to the height of one canopy tier.
- Do **not** recolor the gold to a flat/neon tone, stretch, rotate, or add drop
  shadows.
- On light surfaces (rare), place the mark inside the green tile rather than
  putting bare gold on white.
- Minimum wordmark width ~150px; below that, switch to `logo-mark.svg`.

---

## Iconography — the fintech icon family

One cohesive family so the trade blotter reads as a single system.

**Metrics (identical across every icon):**
- **Grid:** `24×24` viewBox, content kept within ~2–3px padding for even optical size.
- **Stroke:** `stroke-width="1.75"`, `fill="none"`, `stroke-linecap="round"`,
  `stroke-linejoin="round"`.
- **Corner radius:** ~1–2px on rects, matched across the set.
- **Color:** `stroke="currentColor"` for neutral UI icons, so they inherit the
  surrounding text color. **Buy/Sell are the only exceptions** — they hard-code
  green / red so direction is never ambiguous.
- **Feel:** geometric, minimal, single-weight — matching the refined brand.

**The set** (in [`icons/`](./icons/)):

| Icon | Glyph | Color |
|------|-------|-------|
| `buy.svg`          | Rising trend line + up-right arrowhead | Green `#2FB47C` |
| `sell.svg`         | Falling trend line + down-right arrowhead | Red `#E5574E` |
| `trade.svg`        | Two opposing horizontal arrows (an exchange) | `currentColor` |
| `positions.svg`    | Pie / allocation wheel (portfolio holdings) | `currentColor` |
| `blotter.svg`      | Ledger table — header row + rows + a column | `currentColor` |
| `candlestick.svg`  | Two OHLC candles with wicks | `currentColor` |
| `order-ticket.svg` | Order form / ticket with field lines | `currentColor` |
| `pnl.svg`          | Paired up / down arrows (gain-loss delta) | `currentColor` |
| `notional.svg`     | Stacked coins / cylinder (currency value) | `currentColor` |
| `filter.svg`       | Funnel | `currentColor` |
| `sort.svg`         | Descending bars + direction arrow | `currentColor` |
| `search.svg`       | Magnifier | `currentColor` |
| `plus.svg`         | Plus (add a trade) | `currentColor` |

Because neutral icons use `currentColor`, set their color through CSS
(`color: var(--gt-text-muted)`, `var(--gt-gold)` on active, etc.). Buy/Sell keep
their own signal color regardless of context.
