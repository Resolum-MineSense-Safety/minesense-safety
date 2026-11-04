// Stress test (section 4.4.3): keeps adding cabins until the critical path breaks its SLO, to find
// the capacity limit and watch the HorizontalPodAutoscaler react on EKS (kubectl get hpa -w).
// Thresholds do not abort the run: the point is to see where and how it degrades.
//   k6 run -e BASE_URL=http://<alb> tests/performance/k6/stress.js
import { sleep } from 'k6'
import { criticalPath, signIn, uuidv4 } from './lib/minesense.js'

export const options = {
  scenarios: {
    ramp: {
      executor: 'ramping-arrival-rate',
      exec: 'cabin',
      startRate: 10,
      timeUnit: '1s',
      preAllocatedVUs: 50,
      maxVUs: 400,
      stages: [
        { duration: '2m', target: 50 },
        { duration: '2m', target: 100 },
        { duration: '2m', target: 200 },
        { duration: '2m', target: 300 },
        { duration: '2m', target: 0 },
      ],
    },
  },
  thresholds: {
    'http_req_duration{path:critical}': [{ threshold: 'p(95)<500', abortOnFail: false }],
    http_req_failed: [{ threshold: 'rate<0.05', abortOnFail: false }],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
}

export const setup = signIn

export function cabin({ token }) {
  criticalPath(token, uuidv4())
  sleep(0.1)
}

export function handleSummary(data) {
  return { 'tests/performance/results/stress-summary.json': JSON.stringify(data, null, 2) }
}
