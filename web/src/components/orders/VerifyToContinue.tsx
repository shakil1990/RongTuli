import type { ReactNode } from 'react'
import { formatPhoneForDisplay } from '../../lib/phone'
import { useVerifiedPhone } from '../../phone/useVerifiedPhone'
import { PhoneVerification } from '../PhoneVerification'
import { cardSurface } from '../common/styles'

type VerifyToContinueProps = {
  title: string
  description: string
  children: ReactNode
}

/**
 * Orders belong to a phone, not an account, so seeing one means proving the phone again. Once
 * this tab holds a proof, the children render with it and a way to switch numbers.
 */
export function VerifyToContinue({ title, description, children }: VerifyToContinueProps) {
  const { verified, remember, forget } = useVerifiedPhone()

  if (!verified) {
    return (
      <section
        aria-labelledby="verify-heading"
        className={`${cardSurface} mx-auto max-w-xl space-y-4 p-5 sm:p-6`}
      >
        <h1 id="verify-heading" className="text-2xl">
          {title}
        </h1>
        <p className="text-sm text-muted">{description}</p>
        <PhoneVerification onVerified={remember} />
      </section>
    )
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted">
        Showing orders for <strong className="text-ink">{formatPhoneForDisplay(verified.phone)}</strong>.{' '}
        <button
          type="button"
          onClick={forget}
          className="text-accent underline underline-offset-2 hover:text-accent-hover"
        >
          Use a different number
        </button>
      </p>
      {children}
    </div>
  )
}
