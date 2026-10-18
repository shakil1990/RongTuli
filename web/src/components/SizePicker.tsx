type SizePickerProps = {
  sizes: string[]
  value: string | null
  onChange: (size: string) => void
  /** Set when the viewer tried to add to the cart without choosing a size. */
  errorMessage?: string
}

const CHIP = `block cursor-pointer rounded-full border border-line px-4 py-2 text-sm transition
  hover:border-accent
  peer-checked:border-accent peer-checked:bg-accent peer-checked:font-medium peer-checked:text-accent-ink
  peer-focus-visible:ring-2 peer-focus-visible:ring-accent peer-focus-visible:ring-offset-2
  peer-focus-visible:ring-offset-surface`

export function SizePicker({ sizes, value, onChange, errorMessage }: SizePickerProps) {
  return (
    <fieldset aria-describedby={errorMessage ? 'error-size' : undefined}>
      <legend className="text-sm font-medium">Size</legend>
      <div className="mt-2 flex flex-wrap gap-2">
        {sizes.map((size, index) => (
          <div key={size}>
            <input
              type="radio"
              id={`input-size-${index}`}
              name="input-size"
              value={size}
              checked={value === size}
              onChange={() => onChange(size)}
              className="peer sr-only"
            />
            <label htmlFor={`input-size-${index}`} className={CHIP}>
              {size}
            </label>
          </div>
        ))}
      </div>
      {errorMessage && (
        <p id="error-size" role="alert" className="mt-2 text-sm text-accent">
          {errorMessage}
        </p>
      )}
    </fieldset>
  )
}
