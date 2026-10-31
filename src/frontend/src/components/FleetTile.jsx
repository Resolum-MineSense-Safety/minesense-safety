import { Clock, MapPin } from 'lucide-react'
import { ago, label } from '../lib/format'
import { Ring } from './ui'

const BEACON = { Critical: 'bg-risk-critical', Warning: 'bg-risk-warning', Normal: 'bg-risk-normal' }
const WASH = { Critical: 'bg-risk-criticalWash', Warning: 'bg-risk-warningWash', Normal: 'bg-white' }

/** One cab on the fleet board: risk beacon on the left, PERCLOS ring on the right. */
export default function FleetTile({ operator, assessment, onClick, selected }) {
  const risk = operator.currentRiskLevel
  return (
    <button
      type="button"
      onClick={onClick}
      className={`group relative flex w-full items-stretch overflow-hidden rounded-tile border text-left transition-colors ${
        selected ? 'border-basalt ring-2 ring-basalt' : 'border-line hover:border-basalt-mute'
      } ${WASH[risk] ?? 'bg-white'} ${operator.isStale ? 'opacity-70' : ''}`}
    >
      <span className={`w-2 shrink-0 ${BEACON[risk] ?? 'bg-line'} ${risk === 'Critical' ? 'animate-beacon' : ''}`} />
      <div className="flex min-w-0 flex-1 items-center gap-3 p-3">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="rounded-md bg-basalt px-1.5 py-0.5 font-cond text-sm font-semibold text-haul">{operator.vehicleCode}</span>
            <span className="truncate text-sm text-basalt-mute">{operator.fleet}</span>
          </div>
          <div className="mt-1.5 truncate text-[15px] font-semibold">{operator.fullName}</div>
          <div className="mt-1 flex items-center gap-3 text-[13px] text-basalt-mute">
            <span className="flex items-center gap-1"><MapPin className="h-3.5 w-3.5" />{operator.location}</span>
            <span className="flex items-center gap-1"><Clock className="h-3.5 w-3.5" />{ago(operator.lastUpdatedAt)}</span>
          </div>
        </div>
        <Ring value={assessment?.perclos ?? 0} size={56} stroke={6} tone={risk}>
          <span className="numeral text-[15px] leading-none">{assessment ? Math.round(assessment.perclos * 100) : '—'}</span>
        </Ring>
      </div>
      <span className="sr-only">{label(risk)}</span>
    </button>
  )
}
