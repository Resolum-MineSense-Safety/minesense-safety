import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Each bounded context runs as its own .NET service on a local port.
// The dev proxy keeps the browser on a single origin, so no CORS setup is needed.
const services = {
  '/iam': 'http://localhost:5150',
  '/fatigue': 'http://localhost:5158',
  '/alerts': 'http://localhost:5233',
  '/fleet': 'http://localhost:5255',
  '/incidents': 'http://localhost:5070',
}

const proxy = Object.fromEntries(
  Object.entries(services).map(([prefix, target]) => [
    prefix,
    {
      target,
      changeOrigin: true,
      rewrite: (path) => path.replace(new RegExp(`^${prefix}`), ''),
    },
  ]),
)

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: { proxy },
})
