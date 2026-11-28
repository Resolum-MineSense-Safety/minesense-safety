import { Clock, HeartPulse, MapPin, WifiOff } from 'lucide-react'
import { ago, label } from '../lib/format'
import { riskOf } from '../lib/telemetry'
import { Sparkline } from './live'
import { Ring } from './ui'

const BEACON = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal' }
const WASH = { Critical: 'bg-risk-criticalWash', Warning: 'bg-risk-warningWash', Normal: 'bg-white' }

/**
 * One cab on the fleet board: risk beacon on the left, PERCLOS ring on the right and, when its
 * gateway is transmitting, the live PERCLOS line of the last minute and the heart rate.
 */
export default function FleetTile({ operator, assessment, live, onClick, selected }) {
  const risk = operator.currentRiskLevel
  const now = live?.current
  const streaming = live?.state === 'streaming'
  const perclos = streaming && now?.perclos != null ? now.perclos : assessment?.perclos
  return (
    <button
      type="button"
      onClick={onClick}
      className={`group relative flex w-full items-stretch overflow-hidden rounded-tile border text-left transition-colors ${
        selected ? 'border-basalt ring-2 ring-basalt' : 'border-line hover:border-basalt-mute'
      } ${WASH[risk] ?? 'bg-white'} ${operator.isStale ? 'border-dashed saturate-50' : ''}`}
    >
      <span className={`w-2 shrink-0 ${BEACON[risk] ?? 'bg-line'} ${risk === 'Critical' ? 'animate-beacon motion-reduce:animate-none' : ''}`} />
      <div className="flex min-w-0 flex-1 flex-col gap-2 p-3">
        <div className="flex items-center gap-3">
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{operator.vehicleCode}</span>
              <span className="truncate text-sm text-basalt-soft">{operator.fleet}</span>
            </div>
            <div className="mt-1.5 truncate text-[15px] font-semibold">{operator.fullName}</div>
            <div className="mt-1 flex items-center gap-3 text-[13px] text-basalt-soft">
              <span className="flex min-w-0 items-center gap-1"><MapPin className="h-3.5 w-3.5 shrink-0" /><span className="truncate">{live?.location ?? operator.location}</span></span>
              <span className="flex items-center gap-1"><Clock className="h-3.5 w-3.5" />{ago(operator.lastUpdatedAt)}</span>
            </div>
          </div>
          <Ring value={perclos ?? 0} size={56} stroke={6} tone={streaming ? riskOf(now) : risk}>
            <span className="numeral text-[15px] leading-none">{perclos != null ? Math.round(perclos * 100) : '—'}</span>
          </Ring>
        </div>
        {live && (
          <div className="flex items-center gap-3 border-t border-line/70 pt-2">
            {live.state === 'idle' ? (
              <span className="text-[13px] text-basalt-mute">{live.reason}</span>
            ) : (
              <>
                <div className="min-w-0 flex-1">
                  <Sparkline points={live.history} field="perclos" min={0} max={0.6} threshold={0.15} tone={riskOf(now)} height={26} />
                </div>
                {live.state === 'offline' ? (
                  <span className="flex items-center gap-1 text-[13px] font-semibold text-[#8A5200]"><WifiOff className="h-3.5 w-3.5" />Sin cobertura</span>
                ) : (
                  <span className="flex items-center gap-1 text-[13px] text-basalt-soft">
                    <HeartPulse className="h-3.5 w-3.5 text-risk-critical" aria-hidden="true" />
                    <span className="numeral">{now?.heartRate ?? '—'}</span> lpm
                  </span>
                )}
              </>
            )}
          </div>
        )}
      </div>
      <span className="sr-only">{label(risk)}</span>
    </button>
  )
}
