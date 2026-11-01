import { useCallback, useEffect, useRef, useState } from 'react'

/** Loads data from an async function; `reload` repeats it. Optional polling every `interval` ms. */
export function useAsync(fn, deps = [], { interval } = {}) {
  const [state, setState] = useState({ data: undefined, error: null, loading: true })
  const fnRef = useRef(fn)
  fnRef.current = fn

  const reload = useCallback(async () => {
    setState((current) => ({ ...current, loading: true }))
    try {
      const data = await fnRef.current()
      setState({ data, error: null, loading: false })
      return data
    } catch (error) {
      setState((current) => ({ ...current, error, loading: false }))
      return undefined
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  useEffect(() => {
    reload()
    if (!interval) return undefined
    const timer = setInterval(reload, interval)
    return () => clearInterval(timer)
  }, [reload, interval])

  return { ...state, reload, setData: (data) => setState((current) => ({ ...current, data })) }
}

/** Runs a mutation, tracks `busy` and reports the outcome through the toast stack. */
export function useAction(toast) {
  const [busy, setBusy] = useState(null)
  const run = useCallback(
    async (key, fn, success) => {
      setBusy(key)
      try {
        const result = await fn()
        if (success) toast.ok(typeof success === 'function' ? success(result) : success)
        return result
      } catch (error) {
        toast.error(error.message ?? 'No se pudo completar la acción.')
        return undefined
      } finally {
        setBusy(null)
      }
    },
    [toast],
  )
  return { busy, run }
}
