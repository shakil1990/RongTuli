/** Placeholder block shown while a query is pending, so the layout does not jump. */
export function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded-lg bg-sunken ${className}`} aria-hidden="true" />
}
