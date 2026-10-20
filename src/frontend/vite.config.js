import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // DeviceManagementService (launchSettings "http" profile)
      '/api/v1/devices': {
        target: 'http://localhost:5031',
        changeOrigin: true,
      },
    },
  },
})
