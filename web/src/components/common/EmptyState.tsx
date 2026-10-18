import type { ReactNode } from 'react'
import { cardSurface } from './styles'

type EmptyStateProps = {
  title: string
  description: string
  action?: ReactNode
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className={`${cardSurface} flex flex-col items-center gap-3 px-6 py-14 text-center`}>
      <span aria-hidden="true" className="font-display text-4xl text-accent">
        ❧
      </span>
      <h2 className="text-xl">{title}</h2>
      <p className="max-w-sm text-sm text-muted">{description}</p>
      {action}
    </div>
  )
}
