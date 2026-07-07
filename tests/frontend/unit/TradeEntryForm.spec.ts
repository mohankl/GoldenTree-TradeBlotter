import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import TradeEntryForm from '@/components/TradeEntryForm.vue'
import { useBlotterStore } from '@/stores/blotter'

// Mount helper with a fresh Pinia store per test and a stubbed submitTrade action.
function mountForm() {
  const wrapper = mount(TradeEntryForm)
  const store = useBlotterStore()
  const submit = vi.spyOn(store, 'submitTrade').mockResolvedValue({
    id: 't1',
    symbol: 'AAPL',
    side: 'Buy',
    quantity: 100,
    price: 230.5,
    notional: 23050,
    timestamp: new Date().toISOString(),
  })
  return { wrapper, store, submit }
}

async function fill(wrapper: ReturnType<typeof mount>, fields: Record<string, string>) {
  for (const [testid, value] of Object.entries(fields)) {
    await wrapper.find(`[data-testid="${testid}"]`).setValue(value)
  }
}

describe('TradeEntryForm validation (U-F01…U-F06)', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('U-F01: blocks submit and shows an error when symbol is empty', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'quantity-input': '100', 'price-input': '10' })

    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.find('[data-testid="symbol-error"]').exists()).toBe(true)
    expect(submit).not.toHaveBeenCalled()
  })

  it('U-F02: blocks submit when quantity is zero or negative', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'symbol-input': 'AAPL', 'quantity-input': '0', 'price-input': '10' })

    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.find('[data-testid="quantity-error"]').text()).toMatch(/positive/i)
    expect(submit).not.toHaveBeenCalled()
  })

  it('U-F03: blocks submit when price is zero or negative', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'symbol-input': 'AAPL', 'quantity-input': '100', 'price-input': '-5' })

    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.find('[data-testid="price-error"]').text()).toMatch(/positive/i)
    expect(submit).not.toHaveBeenCalled()
  })

  it('U-F04: blocks submit when quantity/price are empty/non-numeric', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'symbol-input': 'AAPL', 'quantity-input': '', 'price-input': '' })

    await wrapper.find('form').trigger('submit.prevent')

    expect(wrapper.find('[data-testid="quantity-error"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="price-error"]').exists()).toBe(true)
    expect(submit).not.toHaveBeenCalled()
  })

  it('U-F05: submits once with the correct payload and resets numeric fields on valid input', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'symbol-input': 'aapl', 'quantity-input': '100', 'price-input': '230.5' })

    await wrapper.find('form').trigger('submit.prevent')
    await new Promise((r) => setTimeout(r, 0)) // let the async submit settle

    expect(submit).toHaveBeenCalledTimes(1)
    expect(submit).toHaveBeenCalledWith({
      symbol: 'AAPL', // normalized to upper-case
      side: 'Buy',
      quantity: 100,
      price: 230.5,
    })

    const qty = wrapper.find('[data-testid="quantity-input"]').element as HTMLInputElement
    const price = wrapper.find('[data-testid="price-input"]').element as HTMLInputElement
    expect(qty.value).toBe('')
    expect(price.value).toBe('')
  })

  it('U-F06: switching the side toggle updates the submitted payload', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'symbol-input': 'MSFT', 'quantity-input': '50', 'price-input': '400' })
    await wrapper.find('[data-testid="side-sell"]').trigger('click')

    await wrapper.find('form').trigger('submit.prevent')
    await new Promise((r) => setTimeout(r, 0))

    expect(submit).toHaveBeenCalledWith(expect.objectContaining({ side: 'Sell', symbol: 'MSFT' }))
  })

  it('U-F07: strips digits/symbols from the ticker as it is entered (letters only)', async () => {
    const { wrapper } = mountForm()
    await fill(wrapper, { 'symbol-input': 'a1a2-pl.' })

    const sym = wrapper.find('[data-testid="symbol-input"]').element as HTMLInputElement
    expect(sym.value).toBe('AAPL')
    expect(wrapper.find('[data-testid="symbol-error"]').exists()).toBe(false)
  })

  it('U-F08: shows a quantity error on entry (before submit) for a non-positive value', async () => {
    const { wrapper, submit } = mountForm()
    await fill(wrapper, { 'quantity-input': '0' })

    // Error is present without ever triggering submit.
    expect(wrapper.find('[data-testid="quantity-error"]').text()).toMatch(/positive/i)
    expect(submit).not.toHaveBeenCalled()
  })

  it('U-F09: shows a price error on entry (before submit) for a non-numeric/empty value', async () => {
    const { wrapper } = mountForm()
    await fill(wrapper, { 'price-input': '10' })
    expect(wrapper.find('[data-testid="price-error"]').exists()).toBe(false)

    await fill(wrapper, { 'price-input': '' })
    expect(wrapper.find('[data-testid="price-error"]').exists()).toBe(true)
  })
})
