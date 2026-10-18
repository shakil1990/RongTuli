import { MAX_CART_QUANTITY } from '../../types/cart'
import { focusRing } from './styles'

type QuantityStepperProps = {
  quantity: number
  label: string
  onChange: (quantity: number) => void
}

const STEP_BUTTON = `flex h-9 w-9 items-center justify-center text-lg leading-none text-muted
  transition hover:bg-sunken hover:text-ink disabled:cursor-not-allowed disabled:opacity-40 ${focusRing}`

export function QuantityStepper({ quantity, label, onChange }: QuantityStepperProps) {
  return (
    <div className="inline-flex items-center rounded-full border border-line bg-raised">
      <button
        type="button"
        className={`${STEP_BUTTON} rounded-l-full`}
        onClick={() => onChange(quantity - 1)}
        disabled={quantity <= 1}
        aria-label={`Reduce the quantity of ${label}`}
      >
        −
      </button>
      <span className="w-9 text-center text-sm font-medium tabular-nums" aria-live="polite">
        {quantity}
      </span>
      <button
        type="button"
        className={`${STEP_BUTTON} rounded-r-full`}
        onClick={() => onChange(quantity + 1)}
        disabled={quantity >= MAX_CART_QUANTITY}
        aria-label={`Increase the quantity of ${label}`}
      >
        +
      </button>
    </div>
  )
}
