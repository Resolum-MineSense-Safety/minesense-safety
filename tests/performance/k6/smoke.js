// Smoke test (section 4.4.3): one virtual user for 30 s right after every deployment, to make
// sure the critical path answers before any heavier test or real traffic.
import { sleep } from 'k6'
import { criticalPath, signIn, supervisorReads, uuidv4 } from './lib/minesense.js'

export const options = {
  vus: 1,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate==0'],
    'http_req_duration{path:critical}': ['p(95)<500'],
  },
}

export const setup = signIn

export default function ({ token }) {
  const operatorId = uuidv4()
  criticalPath(token, operatorId)
  supervisorReads(token, operatorId)
  sleep(1)
}
