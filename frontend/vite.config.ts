import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // 允許 Docker 外的 macOS 瀏覽器連入 Vite 開發伺服器。
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      // 將 /todos 轉送到 Compose 內的 ASP.NET Core app 服務。
      '/todos': {
        target: 'http://app:8080',
        changeOrigin: true,
      },
      // 將會員註冊與後續登入 API 轉送到 ASP.NET Core app 服務。
      '/auth': {
        target: 'http://app:8080',
        changeOrigin: true,
      },
      // 將開發環境的全域例外測試端點轉送到 ASP.NET Core。
      '/debug': {
        target: 'http://app:8080',
        changeOrigin: true,
      },
    },
  },
})
