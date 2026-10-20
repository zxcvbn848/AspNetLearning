<script setup lang="ts">
// ref 建立會驅動畫面更新的測試狀態。
import { ref } from 'vue'
import { ApiError, request } from '../services/apiClient'

const message = ref('')
const traceId = ref('')
const isTesting = ref(false)

/** 呼叫只存在於開發環境的例外端點，確認 Middleware 會回傳 Problem Details。 */
async function testGlobalError(): Promise<void> {
  isTesting.value = true
  message.value = ''
  traceId.value = ''

  try {
    await request('/debug/error')
    message.value = '測試端點沒有產生預期錯誤。'
  } catch (error) {
    if (error instanceof ApiError && error.status === 500) {
      message.value = `已攔截 HTTP 500：${error.message}`
      traceId.value = error.traceId ?? '伺服器未提供 Trace Id。'
    } else {
      message.value = error instanceof Error ? error.message : '發生未預期的錯誤。'
    }
  } finally {
    isTesting.value = false
  }
}
</script>

<template>
  <section class="api-section" aria-labelledby="error-handling-heading">
    <div class="section-heading">
      <div>
        <p class="section-kicker">GET /debug/error</p>
        <h2 id="error-handling-heading">全域例外處理</h2>
      </div>
      <span class="api-badge">Development</span>
    </div>

    <p class="auth-status">觸發預期測試例外，觀察 Problem Details 與 Trace Id。</p>
    <button type="button" class="test-error-button" :disabled="isTesting" @click="testGlobalError">
      {{ isTesting ? '測試中…' : '測試 HTTP 500' }}
    </button>

    <p class="message" aria-live="polite">{{ message }}</p>
    <p v-if="traceId" class="trace-id">Trace Id：{{ traceId }}</p>
  </section>
</template>
