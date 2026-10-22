/** Wires a control to its field's hint or error, so a screen reader reads it with the field. */
export function describedBy(id: string, error: string | undefined, hasHint: boolean) {
  if (error) return { 'aria-invalid': true, 'aria-describedby': `${id}-error` }
  return hasHint ? { 'aria-describedby': `${id}-hint` } : {}
}
