/** A gap stands for the page numbers hidden between two shown ones. */
export const PAGE_GAP = 'gap'

export type PageSlot = number | typeof PAGE_GAP

/**
 * Page numbers to show: always the first and last page plus the current page and its
 * neighbours, with a gap marker wherever numbers were left out.
 */
export function pageWindow(current: number, total: number): PageSlot[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1)
  }

  const shown = new Set<number>([1, total, current])
  if (current - 1 > 1) shown.add(current - 1)
  if (current + 1 < total) shown.add(current + 1)

  const slots: PageSlot[] = []
  let previous = 0
  for (const page of [...shown].sort((a, b) => a - b)) {
    if (previous > 0 && page - previous > 1) {
      slots.push(PAGE_GAP)
    }
    slots.push(page)
    previous = page
  }

  return slots
}
