// Load test (section 4.4.3): the expected peak of a shift change. 40 cabins sending assessments
// every 2 s, plus 10 supervisors polling the control center, for 5 minutes. The thresholds
// are the SLOs of docs/operations/slo.md, so the run fails when a SLO is broken.
//   k6 run -e BASE_URL=http://localhost:8080 tests/performance/k6/load.js
import { sleep } from 'k6'
import { criticalPath, signIn, supervisorReads, uuidv4 } from './lib/minesense.js'

export const options = {
  scenarios: {
    cabins: {
      executor: 'ramping-vus',
      exec: 'cabin',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 40 },
        { duration: '3m', target: 40 },
        { duration: '1m', target: 0 },
      ],
    },
    supervisors: {
      executor: 'constant-vus',
      exec: 'supervisor',
      vus: 10,
      duration: '5m',
    },
  },
  thresholds: {
    'http_req_duration{path:critical}': ['p(95)<500'], // SLO-2 (QAS 02)
    'http_req_duration{path:read}': ['p(95)<2000'], // SLO-3 (QAS 06)
    http_req_failed: ['rate<0.005'], // SLO-1: 99.5 % without errors
    checks: ['rate>0.99'],
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
}

export const setup = signIn

export function cabin({ token }) {
  criticalPath(token, uuidv4())
  sleep(2)
}

export function supervisor({ token }) {
  supervisorReads(token, uuidv4())
  sleep(5)
}

export function handleSummary(data) {
  return {
    'tests/performance/results/load-summary.json': JSON.stringify(data, null, 2),
    stdout: textSummary(data),
  }
}

function textSummary(data) {
  const m = data.metrics
  const line = (name) => {
    const v = m[name]?.values
    return v ? `${name}: p95=${v['p(95)']?.toFixed(1)} ms avg=${v.avg?.toFixed(1)} ms max=${v.max?.toFixed(1)} ms` : `${name}: -`
  }
  return [
    `requests=${m.http_reqs.values.count} rate=${m.http_reqs.values.rate.toFixed(1)}/s failed=${(m.http_req_failed.values.rate * 100).toFixed(2)}%`,
    line('http_req_duration{path:critical}'),
    line('http_req_duration{path:read}'),
    `checks=${(m.checks.values.rate * 100).toFixed(2)}%`,
    '',
  ].join('\n')
}
