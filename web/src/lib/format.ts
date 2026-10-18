/**
 * Prices are Bangladeshi taka. Columns hold two decimals, but the shop quotes whole taka,
 * so decimals are shown only when a price actually has them.
 */
const wholeTaka = new Intl.NumberFormat('en-BD', { maximumFractionDigits: 0 })
const fractionalTaka = new Intl.NumberFormat('en-BD', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function formatTaka(amount: number): string {
  const formatter = Number.isInteger(amount) ? wholeTaka : fractionalTaka
  return `৳${formatter.format(amount)}`
}

/** Initials for the image placeholder shown until Step 5 adds real product photos. */
export function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter((word) => word.length > 0)
    .slice(0, 2)
    .map((word) => word[0]!.toUpperCase())
    .join('')
}
