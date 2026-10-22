/** Moves one entry a step towards the start (-1) or the end (+1); out-of-range moves are no-ops. */
export function moveItem<T>(items: readonly T[], index: number, step: -1 | 1): T[] {
  const moved = [...items]
  const target = index + step
  if (index < 0 || index >= items.length || target < 0 || target >= items.length) {
    return moved
  }

  ;[moved[index], moved[target]] = [moved[target]!, moved[index]!]
  return moved
}

/** Moves one entry to the front, keeping the others in their order. */
export function moveToFront<T>(items: readonly T[], index: number): T[] {
  if (index <= 0 || index >= items.length) {
    return [...items]
  }
  return [items[index]!, ...items.slice(0, index), ...items.slice(index + 1)]
}
