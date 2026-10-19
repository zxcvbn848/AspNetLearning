<script setup lang="ts">
import { ref } from 'vue'
import { loginUser } from '../services/authApi'

const email = ref('')
const password = ref('')
const message = ref('')
const isSuccess = ref(false)
const isSubmitting = ref(false)

/** 呼叫登入 API，並在目前瀏覽器分頁保存 JWT。 */
async function submitLogin(): Promise<void> {
  isSubmitting.value = true
  showMessage('')

  try {
    const result = await loginUser(email.value.trim(), password.value)

    // sessionStorage 關閉分頁後就會清除；下一階段會用它呼叫受保護 API。
    window.sessionStorage.setItem('accessToken', result.accessToken)
    password.value = ''

    const expiresAt = new Date(result.expiresAtUtc).toLocaleString('zh-TW')
    showMessage(`歡迎 ${result.user.email}，Token 有效至 ${expiresAt}。`, true)
  } catch (error) {
    window.sessionStorage.removeItem('accessToken')
    showMessage(error instanceof Error ? error.message : '發生未預期的錯誤。')
  } finally {
    isSubmitting.value = false
  }
}

/** 統一更新登入結果文字與顯示顏色。 */
function showMessage(text: string, success = false): void {
  message.value = text
  isSuccess.value = success
}
</script>

<template>
  <section class="api-section" aria-labelledby="login-heading">
    <div class="section-heading">
      <div>
        <p class="section-kicker">POST /auth/login</p>
        <h2 id="login-heading">會員登入</h2>
      </div>
      <span class="api-badge">JWT</span>
    </div>

    <form class="auth-form" @submit.prevent="submitLogin">
      <div class="form-field">
        <label for="login-email">Email</label>
        <input
          id="login-email"
          v-model="email"
          type="email"
          maxlength="254"
          autocomplete="email"
          placeholder="andy@example.com"
          required
        >
      </div>

      <div class="form-field">
        <label for="login-password">密碼</label>
        <input
          id="login-password"
          v-model="password"
          type="password"
          maxlength="100"
          autocomplete="current-password"
          placeholder="輸入密碼"
          required
        >
      </div>

      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? '登入中…' : '登入並取得 Token' }}
      </button>
    </form>

    <p class="message" :class="{ success: isSuccess }" aria-live="polite">
      {{ message }}
    </p>
  </section>
</template>
