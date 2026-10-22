import type { ReactNode } from 'react'
import { fieldError } from './styles'

type FieldProps = {
  id: string
  label: string
  hint?: string
  error?: string
  children: ReactNode
}

/** A labelled form control with its hint, or its error in place of the hint. */
export function Field({ id, label, hint, error, children }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="block text-sm font-medium">
        {label}
      </label>
      {children}
      {hint && !error && (
        <p id={`${id}-hint`} className="text-xs text-muted">
          {hint}
        </p>
      )}
      {error && (
        <p id={`${id}-error`} className={fieldError}>
          {error}
        </p>
      )}
    </div>
  )
}
