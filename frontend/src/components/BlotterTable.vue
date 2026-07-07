<script setup lang="ts">
import { computed, ref } from 'vue'
import { useBlotterStore } from '../stores/blotter'
import { currency, qtyFmt } from '../format'
import type { Trade } from '../types'

const store = useBlotterStore()

type SortKey = 'timestamp' | 'symbol' | 'quantity' | 'price' | 'notional'
type SortDir = 'asc' | 'desc'

const sortKey = ref<SortKey>('timestamp')
const sortDir = ref<SortDir>('desc')

function toggleSort(key: SortKey) {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortKey.value = key
    sortDir.value = key === 'symbol' ? 'asc' : 'desc'
  }
}

const sortedTrades = computed<Trade[]>(() => {
  const dir = sortDir.value === 'asc' ? 1 : -1
  return [...store.trades].sort((a, b) => {
    let cmp: number
    switch (sortKey.value) {
      case 'symbol':
        cmp = a.symbol.localeCompare(b.symbol)
        break
      case 'timestamp':
        cmp = new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
        break
      default:
        cmp = a[sortKey.value] - b[sortKey.value]
    }
    return cmp * dir
  })
})

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

function ariaSort(key: SortKey): 'ascending' | 'descending' | 'none' {
  if (sortKey.value !== key) return 'none'
  return sortDir.value === 'asc' ? 'ascending' : 'descending'
}

function caret(key: SortKey): string {
  if (sortKey.value !== key) return ''
  return sortDir.value === 'asc' ? '▲' : '▼'
}
</script>

<template>
  <section class="panel blotter">
    <header class="panel__header">
      <span class="panel__title">Blotter</span>
      <span class="panel__meta">{{ store.trades.length }} trades · newest first</span>
    </header>

    <div class="scroll">
      <table>
        <thead>
          <tr>
            <th
              scope="col"
              class="th th--l th--sortable"
              :aria-sort="ariaSort('timestamp')"
              data-testid="sort-timestamp"
              @click="toggleSort('timestamp')"
            >
              Time <span class="caret">{{ caret('timestamp') }}</span>
            </th>
            <th
              scope="col"
              class="th th--l th--sortable"
              :aria-sort="ariaSort('symbol')"
              @click="toggleSort('symbol')"
            >
              Symbol <span class="caret">{{ caret('symbol') }}</span>
            </th>
            <th scope="col" class="th th--l">Side</th>
            <th
              scope="col"
              class="th th--sortable"
              :aria-sort="ariaSort('quantity')"
              @click="toggleSort('quantity')"
            >
              Qty <span class="caret">{{ caret('quantity') }}</span>
            </th>
            <th
              scope="col"
              class="th th--sortable"
              :aria-sort="ariaSort('price')"
              @click="toggleSort('price')"
            >
              Price <span class="caret">{{ caret('price') }}</span>
            </th>
            <th
              scope="col"
              class="th th--sortable"
              :aria-sort="ariaSort('notional')"
              data-testid="sort-notional"
              @click="toggleSort('notional')"
            >
              Notional <span class="caret">{{ caret('notional') }}</span>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="trade in sortedTrades"
            :key="trade.id"
            class="row"
            :class="trade.side === 'Buy' ? 'row--buy' : 'row--sell'"
            data-testid="trade-row"
          >
            <td class="td td--l td--time">{{ formatTime(trade.timestamp) }}</td>
            <td class="td td--l td--sym" data-testid="trade-symbol">{{ trade.symbol }}</td>
            <td class="td td--l">
              <span class="side-pill" :class="trade.side === 'Buy' ? 'side-pill--buy' : 'side-pill--sell'">
                {{ trade.side.toUpperCase() }}
              </span>
            </td>
            <td class="td num">{{ qtyFmt.format(trade.quantity) }}</td>
            <td class="td num">{{ currency.format(trade.price) }}</td>
            <td class="td num td--notional">{{ currency.format(trade.notional) }}</td>
          </tr>

          <tr v-if="!store.loading && store.trades.length === 0">
            <td class="td td--empty" colspan="6">No trades yet — book one to get started.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<style scoped>
.blotter {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.scroll {
  overflow: auto;
  max-height: 72vh;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.th {
  position: sticky;
  top: 0;
  z-index: 1;
  background: var(--surface);
  color: var(--muted);
  text-align: right;
  font-family: var(--mono);
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  padding: 9px 16px;
  border-bottom: 1px solid var(--line);
  white-space: nowrap;
  user-select: none;
}

.th--l {
  text-align: left;
}

.th--sortable {
  cursor: pointer;
}

.th--sortable:hover {
  color: var(--text);
}

.caret {
  color: var(--gold);
  font-size: 9px;
}

.td {
  padding: 10px 16px;
  border-bottom: 1px solid var(--line-soft);
  text-align: right;
  white-space: nowrap;
  color: var(--text);
}

.td--l {
  text-align: left;
}

.row td:first-child {
  border-left: 3px solid transparent;
}
.row--buy td:first-child {
  border-left-color: var(--buy);
}
.row--sell td:first-child {
  border-left-color: var(--sell);
}
.row:hover td {
  background: #182019;
}

.td--time {
  font-family: var(--mono);
  font-size: 11.5px;
  color: var(--muted);
}

.td--sym {
  font-weight: 650;
  letter-spacing: 0.02em;
}

.td--notional {
  color: var(--gold-bright);
}

.td--empty {
  text-align: center;
  color: var(--faint);
  padding: 28px 16px;
}
</style>
