import { CheckCircle2, AlertTriangle } from 'lucide-react'
import { createContext, useCallback, useContext, useMemo, useState } from 'react'

const ToastContext = createContext(null)

export function ToastProvider({ children }) {
  const [items, setItems] = useState([])
  const push = useCallback((tone, text) => {
    const id = Math.random().toString(36).slice(2)
    setItems((current) => [...current, { id, tone, text }])
    setTimeout(() => setItems((current) => current.filter((item) => item.id !== id)), tone === 'error' ? 6000 : 3200)
  }, [])
  const api = useMemo(() => ({ ok: (text) => push('ok', text), error: (text) => push('error', text) }), [push])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div aria-live="polite" className="fixed bottom-5 right-5 z-50 flex w-80 flex-col gap-2">
        {items.map((item) => (
          <div
            key={item.id}
            className={`flex animate-slidein items-start gap-2 rounded-xl px-4 py-3 text-[14px] font-medium text-white shadow-lg ${
              item.tone === 'ok' ? 'bg-basalt' : 'bg-risk-critical'
            }`}
          >
            {item.tone === 'ok' ? <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-haul" /> : <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />}
            <span>{item.text}</span>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export const useToast = () => useContext(ToastContext)
