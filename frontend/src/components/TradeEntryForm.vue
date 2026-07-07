<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useBlotterStore } from '../stores/blotter'
import { ApiError } from '../api/client'
import type { Side, TradeInput } from '../types'

const store = useBlotterStore()

const form = reactive({
  symbol: '',
  side: 'Buy' as Side,
  quantity: '' as number | string,
  price: '' as number | string,
})

const errors = reactive<Record<string, string>>({})
const flash = ref<string | null>(null)

/** Validate a numeric field that may arrive as a number, '', or a raw string. */
function validateNumber(raw: number | string, label: string): string | null {
  const text = String(raw ?? '').trim()
  if (text === '') return `${label} must be a number.`
  const value = Number(text)
  if (Number.isNaN(value)) return `${label} must be a number.`
  if (value <= 0) return `${label} must be positive.`
  return null
}

function validate(): boolean {
  for (const key of Object.keys(errors)) delete errors[key]

  if (!String(form.symbol).trim()) {
    errors.symbol = 'Symbol is required.'
  }

  const qtyError = validateNumber(form.quantity, 'Quantity')
  if (qtyError) errors.quantity = qtyError

  const priceError = validateNumber(form.price, 'Price')
  if (priceError) errors.price = priceError

  return Object.keys(errors).length === 0
}

async function onSubmit() {
  flash.value = null
  if (!validate()) return

  const payload: TradeInput = {
    symbol: String(form.symbol).trim().toUpperCase(),
    side: form.side,
    quantity: Number(form.quantity),
    price: Number(form.price),
  }

  try {
    const trade = await store.submitTrade(payload)
    flash.value = `${trade.side} ${trade.quantity} ${trade.symbol} @ ${trade.price} booked.`
    form.quantity = ''
    form.price = ''
  } catch (e) {
    if (e instanceof ApiError) {
      for (const [field, messages] of Object.entries(e.fieldErrors)) {
        errors[field] = messages[0]
      }
    }
  }
}
</script>

<template>
  <form class="ticket" novalidate @submit.prevent="onSubmit">
    <!-- Symbol -->
    <div class="fld fld--sym">
      <label for="t-sym">Symbol</label>
      <input
        id="t-sym"
        v-model="form.symbol"
        class="input"
        type="text"
        placeholder="AAPL"
        autocomplete="off"
        spellcheck="false"
        data-testid="symbol-input"
        @input="form.symbol = String(form.symbol).toUpperCase()"
      />
      <span v-if="errors.symbol" class="err" data-testid="symbol-error">{{ errors.symbol }}</span>
    </div>

    <!-- Side -->
    <div class="fld fld--side">
      <label>Side</label>
      <div class="seg" role="group" aria-label="Side">
        <button
          type="button"
          class="seg__btn"
          :class="{ 'is-on-buy': form.side === 'Buy' }"
          data-testid="side-buy"
          @click="form.side = 'Buy'"
        >
          Buy
        </button>
        <button
          type="button"
          class="seg__btn"
          :class="{ 'is-on-sell': form.side === 'Sell' }"
          data-testid="side-sell"
          @click="form.side = 'Sell'"
        >
          Sell
        </button>
      </div>
    </div>

    <!-- Quantity -->
    <div class="fld">
      <label for="t-qty">Quantity</label>
      <input
        id="t-qty"
        v-model="form.quantity"
        class="input num"
        type="number"
        min="0"
        step="any"
        inputmode="decimal"
        placeholder="100"
        data-testid="quantity-input"
      />
      <span v-if="errors.quantity" class="err" data-testid="quantity-error">{{ errors.quantity }}</span>
    </div>

    <!-- Price -->
    <div class="fld">
      <label for="t-px">Price</label>
      <input
        id="t-px"
        v-model="form.price"
        class="input num"
        type="number"
        min="0"
        step="any"
        inputmode="decimal"
        placeholder="210.50"
        data-testid="price-input"
      />
      <span v-if="errors.price" class="err" data-testid="price-error">{{ errors.price }}</span>
    </div>

    <!-- Submit -->
    <div class="fld fld--action">
      <button class="book" type="submit" :disabled="store.submitting" data-testid="submit-trade">
        {{ store.submitting ? 'Booking…' : 'Book Trade' }}
      </button>
    </div>

    <p v-if="flash" class="flash" data-testid="submit-flash">{{ flash }}</p>
  </form>
</template>

<style scoped>
.ticket {
  display: grid;
  grid-template-columns: 1.4fr auto 1fr 1fr auto;
  gap: 12px;
  align-items: end;
  background: linear-gradient(180deg, var(--surface), var(--panel));
  border: 1px solid var(--line);
  border-top: 2px solid var(--gold);
  border-radius: var(--r);
  padding: 14px 16px;
  margin-bottom: 18px;
}

.flash {
  grid-column: 1 / -1;
  margin: 0;
  font-family: var(--mono);
  font-size: 12px;
  color: var(--buy);
}

.fld {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.fld > label {
  font-family: var(--mono);
  font-size: 10px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--muted);
}

.err {
  position: absolute;
  margin-top: 68px;
  font-size: 11px;
  color: var(--sell);
}

.input {
  background: var(--bg);
  border: 1px solid var(--line);
  border-radius: var(--r-sm);
  color: var(--text);
  padding: 10px 12px;
  font-size: 15px;
  font-family: var(--mono);
  outline: none;
  width: 100%;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.input::placeholder {
  color: var(--faint);
}

.input:focus {
  border-color: var(--gold);
  box-shadow: 0 0 0 3px rgba(201, 162, 39, 0.14);
}

.seg {
  display: inline-flex;
  background: var(--bg);
  border: 1px solid var(--line);
  border-radius: var(--r-sm);
  padding: 3px;
  gap: 3px;
}

.seg__btn {
  border: 0;
  background: transparent;
  color: var(--muted);
  font-weight: 650;
  font-size: 14px;
  padding: 7px 18px;
  border-radius: 6px;
  transition: all 0.15s;
}

.seg__btn.is-on-buy {
  color: var(--buy);
  background: var(--buy-dim);
  box-shadow: inset 0 0 0 1px rgba(47, 180, 124, 0.4);
}

.seg__btn.is-on-sell {
  color: var(--sell);
  background: var(--sell-dim);
  box-shadow: inset 0 0 0 1px rgba(229, 87, 78, 0.4);
}

.book {
  border: 0;
  border-radius: var(--r-sm);
  padding: 11px 26px;
  font-weight: 700;
  font-size: 14px;
  letter-spacing: 0.01em;
  color: #14100a;
  height: 44px;
  white-space: nowrap;
  background: linear-gradient(180deg, var(--gold-bright), var(--gold));
  transition: filter 0.15s, opacity 0.15s;
}

.book:hover:not(:disabled) {
  filter: brightness(1.07);
}

.book:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 940px) {
  .ticket {
    grid-template-columns: 1fr 1fr;
  }
  .fld--action {
    grid-column: 1 / -1;
  }
  .book {
    width: 100%;
  }
  .err {
    position: static;
    margin-top: 0;
  }
}
</style>
