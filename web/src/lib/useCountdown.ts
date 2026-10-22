import { useCallback, useEffect, useState } from 'react'

/**
 * Whole seconds left until a deadline, ticking once a second while there is time left. Kept as a
 * deadline rather than a counter so a throttled background tab still shows the right time when
 * it comes back.
 */
export function useCountdown() {
  const [deadline, setDeadline] = useState<number | null>(null)
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    if (deadline === null) return
    const timer = window.setInterval(() => {
      const current = Date.now()
      setNow(current)
      if (current >= deadline) window.clearInterval(timer)
    }, 1000)
    return () => window.clearInterval(timer)
  }, [deadline])

  const start = useCallback((seconds: number) => {
    const current = Date.now()
    setNow(current)
    setDeadline(current + seconds * 1000)
  }, [])

  const secondsLeft = deadline === null ? 0 : Math.max(0, Math.ceil((deadline - now) / 1000))
  return { secondsLeft, isRunning: secondsLeft > 0, start }
}
