import { readFileSync } from 'node:fs'
import process from 'node:process'
import { fileURLToPath } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Local `dotnet run` port of each microservice (Properties/launchSettings.json).
const SERVICE_PORTS = {
  'identity-access-service': 5150,
  'operations-service': 5103,
  'device-management-service': 5031,
  'monitoring-service': 5101,
  'fatigue-detection-service': 5158,
  'alert-service': 5233,
  'fleet-monitoring-service': 5255,
  'incident-management-service': 5070,
  'privacy-audit-service': 5260,
  'reporting-service': 5270,
  'analytics-service': 5090,
  'edge-synchronization-service': 5280,
  'feedback-reliability-service': 5281,
}

/**
 * Same routing as the ALB Ingress and the docker-compose gateway: the rules come from the Helm
 * chart minesense-gateway, in order, so the first matching prefix wins. GATEWAY_URL sends every
 * rule to one gateway instead (for example http://localhost:8080 with docker compose).
 */
function gatewayProxy() {
  let values
  try {
    values = readFileSync(
      fileURLToPath(new URL('../../infrastructure/helm/minesense-gateway/values.yaml', import.meta.url)), 'utf8')
  } catch {
    // Outside the monorepo (e.g. the Docker build context) there is no dev server to proxy.
    return {}
  }
  const proxy = {}
  let path = null
  for (const line of values.split(/\r?\n/).map((l) => l.trim())) {
    if (line.startsWith('- path:')) path = line.slice('- path:'.length).trim()
    else if (line.startsWith('service:') && path) {
      const service = line.slice('service:'.length).trim()
      // Escape every regex metacharacter of the path; "*" is the one-segment wildcard of the ALB rules.
      const literal = path.split('*').map((part) => part.replace(/[\\^$.+?()[\]{}|]/g, '\\$&'))
      const pattern = '^' + literal.join('[^/]+') + '(/.*)?(\\?.*)?$'
      const target = process.env.GATEWAY_URL || `http://localhost:${SERVICE_PORTS[service]}`
      proxy[pattern] = { target, changeOrigin: true }
      path = null
    }
  }
  return proxy
}

export default defineConfig({
  plugins: [react()],
  server: { port: 5173, proxy: gatewayProxy() },
  preview: { port: 4173, proxy: gatewayProxy() },
})
