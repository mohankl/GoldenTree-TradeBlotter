# brand-assets/

Brand assets and icons for the **GoldenTree Trade Blotter** web app. All art is
**original**, created for this project and visually inspired by — not copied
from — GoldenTree Asset Management's institutional aesthetic (refined gold +
deep forest green + dark neutrals). See [`brand-guidelines.md`](./brand-guidelines.md)
for the full system and the "source & inspiration" note.

## Contents

| File | What it is |
|------|-----------|
| `brand-guidelines.md` | Full brand system: palette, typography, logo & icon rules. |
| `palette.css` | CSS custom properties (`--gt-*`) — the design tokens. Stable names. |
| `logo.svg` | Horizontal wordmark (mark + "GoldenTree"), for the header. |
| `logo-mark.svg` | Mark only (layered golden conifer), for compact spots. |
| `favicon.svg` | Bold, simplified mark for 16–32px browser tabs. |
| `icon-192.svg` | PWA app icon, 192px, mark on a forest-green tile. |
| `icon-512.svg` | PWA app icon, 512px, mark on a forest-green tile. |

### Fintech icon family — `icons/` (24×24, 1.75px stroke, one visual system)

| File | What it is |
|------|-----------|
| `icons/buy.svg`          | Rising trend line + arrow — Buy side (**green**). |
| `icons/sell.svg`         | Falling trend line + arrow — Sell side (**red**). |
| `icons/trade.svg`        | Opposing exchange arrows — a trade (`currentColor`). |
| `icons/positions.svg`    | Pie / allocation wheel — portfolio holdings (`currentColor`). |
| `icons/blotter.svg`      | Ledger table (rows) — the blotter (`currentColor`). |
| `icons/candlestick.svg`  | OHLC candles — price chart (`currentColor`). |
| `icons/order-ticket.svg` | Order form / ticket (`currentColor`). |
| `icons/pnl.svg`          | Up / down delta — P&L (`currentColor`). |
| `icons/notional.svg`     | Stacked coins — notional / value (`currentColor`). |
| `icons/filter.svg`       | Funnel — filter (`currentColor`). |
| `icons/sort.svg`         | Bars + arrow — sort (`currentColor`). |
| `icons/search.svg`       | Magnifier — search (`currentColor`). |
| `icons/plus.svg`         | Plus — add trade (`currentColor`). |

All icons share the same grid, 1.75px stroke, round caps/joins, and corner
radius, so they read as one family. Buy/Sell are the only icons that hard-code
color (green / red) for unambiguous direction; everything else inherits
`currentColor`.

## How the frontend should consume it

1. **Palette** — import the tokens once, globally (e.g. in `main.ts`/`main.js`
   or your root stylesheet), then use `var(--gt-*)` everywhere:
   ```css
   @import "../brand-assets/palette.css";
   ```
   (Adjust the relative path to where the file sits relative to your app.)

2. **Header logo** — inline or `<img>` the wordmark:
   ```html
   <img src="/brand-assets/logo.svg" alt="GoldenTree Trade Blotter" height="32" />
   ```
   Use `logo-mark.svg` in collapsed / narrow layouts.

3. **Favicon + PWA** — in `index.html` / web manifest:
   ```html
   <link rel="icon" type="image/svg+xml" href="/brand-assets/favicon.svg" />
   ```
   ```json
   { "icons": [
       { "src": "/brand-assets/icon-192.svg", "sizes": "192x192", "type": "image/svg+xml" },
       { "src": "/brand-assets/icon-512.svg", "sizes": "512x512", "type": "image/svg+xml" }
   ] }
   ```

4. **UI icons** — **inline** the SVGs (so `currentColor` and `stroke` inherit
   from your CSS), then color via `color: var(--gt-...)`. Buy/Sell carry their
   own green/red; neutral icons follow the surrounding text color.

## Color swatches

| Token | Hex |
|-------|-----|
| `--gt-gold` | `#C9A227` |
| `--gt-gold-bright` | `#E3C15A` |
| `--gt-green` | `#12563B` |
| `--gt-green-mid` | `#1B6B4A` |
| `--gt-buy` | `#2FB47C` |
| `--gt-sell` | `#E5574E` |
| `--gt-bg` | `#0A0E0C` |
| `--gt-surface` | `#151B16` |
| `--gt-surface-2` | `#1E261F` |
| `--gt-border` | `#2A352C` |
| `--gt-text` | `#E8EDE9` |
| `--gt-text-muted` | `#9AA79C` |

Full token list with usage notes: [`brand-guidelines.md`](./brand-guidelines.md).

> **Note:** these are original works inspired by GoldenTree Asset Management's
> public aesthetic; they are not the firm's trademarked logo or brand files.
