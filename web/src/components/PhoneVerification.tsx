import { useState, type FormEvent } from 'react'
import { ApiError } from '../lib/api'
import {
  cleanCodeInput,
  formatCountdown,
  formatPhoneForDisplay,
  formatPhoneInput,
  toE164,
} from '../lib/phone'
import { useCountdown } from '../lib/useCountdown'
import { sendPhoneCode, verifyPhoneCode } from '../services/phone'
import type { CodeSent, VerifiedPhone } from '../types/phone'
import { Button } from './common/Button'
import { alertBanner, fieldControl, fieldError } from './common/styles'

const PHONE_HINT = 'A Bangladeshi mobile number, like 01712-345678.'
const CODE_LENGTH = 6
const OFFLINE = 'We could not reach the shop. Check your connection and try again.'

type PhoneVerificationProps = {
  onVerified: (verified: VerifiedPhone) => void
}

/** The message to show for a failed call, and the field it belongs beside if it has one. */
function readFailure(caught: unknown, field: 'phone' | 'code') {
  if (!(caught instanceof ApiError)) return { fieldMessage: null, problem: OFFLINE }
  const fieldMessage = caught.fieldErrors[field]?.[0] ?? null
  return { fieldMessage, problem: fieldMessage ? null : caught.message }
}

/**
 * Proves the customer holds a phone, without leaving the page: send a code, type it in, get a
 * token. Everything is in one panel so the cart and anything typed on the page survive it.
 */
export function PhoneVerification({ onVerified }: PhoneVerificationProps) {
  const [phoneInput, setPhoneInput] = useState('')
  const [sent, setSent] = useState<CodeSent | null>(null)
  const [code, setCode] = useState('')
  const [phoneMessage, setPhoneMessage] = useState<string | null>(null)
  const [codeMessage, setCodeMessage] = useState<string | null>(null)
  const [problem, setProblem] = useState<string | null>(null)
  const [isSending, setIsSending] = useState(false)
  const [isVerifying, setIsVerifying] = useState(false)
  const resend = useCountdown()
  const expiry = useCountdown()

  const isCodeExpired = sent !== null && !expiry.isRunning

  const sendCode = async () => {
    setProblem(null)
    setPhoneMessage(null)
    setCodeMessage(null)

    const phone = sent?.phone ?? toE164(phoneInput)
    if (!phone) {
      setPhoneMessage(`Enter a complete mobile number. ${PHONE_HINT}`)
      return
    }

    setIsSending(true)
    try {
      const result = await sendPhoneCode(phone)
      setSent(result)
      setCode('')
      resend.start(result.resendAfterSeconds)
      expiry.start(result.expiresInSeconds)
    } catch (caught) {
      // A refused send still says how long to wait, and the button counts it down.
      if (caught instanceof ApiError && caught.retryAfterSeconds !== null) {
        resend.start(caught.retryAfterSeconds)
      }
      const { fieldMessage, problem: message } = readFailure(caught, 'phone')
      setPhoneMessage(fieldMessage)
      setProblem(message)
    } finally {
      setIsSending(false)
    }
  }

  const handleSend = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void sendCode()
  }

  const handleVerify = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!sent) return
    setProblem(null)

    if (code.length !== CODE_LENGTH) {
      setCodeMessage(`Enter all ${CODE_LENGTH} digits from the text message.`)
      return
    }

    setIsVerifying(true)
    try {
      onVerified(await verifyPhoneCode(sent.phone, code))
    } catch (caught) {
      const { fieldMessage, problem: message } = readFailure(caught, 'code')
      setCodeMessage(fieldMessage)
      setProblem(message)
    } finally {
      setIsVerifying(false)
    }
  }

  const changeNumber = () => {
    setSent(null)
    setCode('')
    setCodeMessage(null)
    setProblem(null)
  }

  const sendLabel = isSending
    ? 'Sending…'
    : resend.isRunning
      ? `${sent ? 'Send again' : 'Send code'} in ${formatCountdown(resend.secondsLeft)}`
      : sent
        ? 'Send a new code'
        : 'Send code'

  return (
    <div className="space-y-4">
      {problem && (
        <p role="alert" className={alertBanner}>
          {problem}
        </p>
      )}

      {sent === null ? (
        <form onSubmit={handleSend} noValidate className="space-y-3">
          <div className="space-y-1.5">
            <label htmlFor="checkout-phone" className="block text-sm font-medium">
              Mobile number
            </label>
            <input
              id="checkout-phone"
              name="phone"
              type="tel"
              inputMode="numeric"
              autoComplete="tel-national"
              placeholder="01712-345678"
              value={phoneInput}
              onChange={(event) => {
                setPhoneInput(formatPhoneInput(event.target.value))
                setPhoneMessage(null)
              }}
              className={fieldControl}
              aria-invalid={phoneMessage ? true : undefined}
              aria-describedby={phoneMessage ? 'checkout-phone-error' : 'checkout-phone-hint'}
            />
            {phoneMessage ? (
              <p id="checkout-phone-error" className={fieldError}>
                {phoneMessage}
              </p>
            ) : (
              <p id="checkout-phone-hint" className="text-xs text-muted">
                We text a {CODE_LENGTH}-digit code to confirm it is yours. No account needed.
              </p>
            )}
          </div>
          <Button type="submit" disabled={isSending || resend.isRunning}>
            {sendLabel}
          </Button>
        </form>
      ) : (
        <form onSubmit={handleVerify} noValidate className="space-y-3">
          <p className="text-sm">
            We sent a code to <strong>{formatPhoneForDisplay(sent.phone)}</strong>.{' '}
            <button
              type="button"
              onClick={changeNumber}
              className="text-accent underline underline-offset-2 hover:text-accent-hover"
            >
              Change number
            </button>
          </p>
          <div className="space-y-1.5">
            <label htmlFor="checkout-code" className="block text-sm font-medium">
              Code
            </label>
            <input
              id="checkout-code"
              name="code"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={CODE_LENGTH}
              placeholder="123456"
              value={code}
              onChange={(event) => {
                setCode(cleanCodeInput(event.target.value))
                setCodeMessage(null)
              }}
              className={`${fieldControl} max-w-40 font-mono tracking-[0.3em]`}
              aria-invalid={codeMessage ? true : undefined}
              aria-describedby={codeMessage ? 'checkout-code-error' : 'checkout-code-hint'}
            />
            {codeMessage ? (
              <p id="checkout-code-error" className={fieldError}>
                {codeMessage}
              </p>
            ) : (
              <p id="checkout-code-hint" className="text-xs text-muted" aria-live="polite">
                {isCodeExpired
                  ? 'This code has expired. Send a new one.'
                  : `Expires in ${formatCountdown(expiry.secondsLeft)}.`}
              </p>
            )}
          </div>
          <div className="flex flex-wrap gap-3">
            <Button type="submit" disabled={isVerifying || isCodeExpired}>
              {isVerifying ? 'Checking…' : 'Verify'}
            </Button>
            <Button
              variant="secondary"
              onClick={() => void sendCode()}
              disabled={isSending || resend.isRunning}
            >
              {sendLabel}
            </Button>
          </div>
        </form>
      )}
    </div>
  )
}
