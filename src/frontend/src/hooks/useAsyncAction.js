import { useCallback, useState } from 'react'

/**
 * Wraps an async function with `pending` and `error` state.
 * `run` resolves to the function result, or `undefined` when it failed.
 */
export function useAsyncAction(action) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState(null)

  const run = useCallback(
    async (...args) => {
      setPending(true)
      setError(null)
      try {
        return await action(...args)
      } catch (err) {
        setError(err?.message ?? 'Error inesperado.')
        return undefined
      } finally {
        setPending(false)
      }
    },
    [action],
  )

  const clearError = useCallback(() => setError(null), [])

  return { run, pending, error, clearError }
}

export const newGuid = () =>
  typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : ''

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

export const isGuid = (value) => guidPattern.test(String(value ?? '').trim())
