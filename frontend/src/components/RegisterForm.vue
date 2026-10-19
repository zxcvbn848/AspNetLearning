<script setup lang="ts">
import { ref } from 'vue'
import { registerUser } from '../services/authApi'

const email = ref('')
const password = ref('')
const passwordConfirmation = ref('')
const message = ref('')
const isSuccess = ref(false)
const isSubmitting = ref(false)

/** 驗證兩次密碼後，呼叫會員註冊 API。 */
async function submitRegistration(): Promise<void> {
  if (password.value !== passwordConfirmation.value) {
    showMessage('兩次輸入的密碼不一致。')
    return
  }

  isSubmitting.value = true
  showMessage('')

  try {
    const user = await registerUser(email.value.trim(), password.value)
    email.value = ''
    password.value = ''
    passwordConfirmation.value = ''
    showMessage(`會員 ${user.email} 註冊成功，會員編號為 ${user.id}。`, true)
  } catch (error) {
    showMessage(error instanceof Error ? error.message : '發生未預期的錯誤。')
  } finally {
    isSubmitting.value = false
  }
}

/** 統一更新註冊結果文字與顯示顏色。 */
function showMessage(text: string, success = false): void {
  message.value = text
  isSuccess.value = success
}
</script>

<template>
  <section class="api-section" aria-labelledby="register-heading">
    <div class="section-heading">
      <div>
        <p class="section-kicker">POST /auth/register</p>
        <h2 id="register-heading">會員註冊</h2>
      </div>
      <span class="api-badge">會員 API</span>
    </div>

    <!-- submit.prevent 呼叫 Vue 函式，避免瀏覽器重新整理整頁。 -->
    <form class="register-form" @submit.prevent="submitRegistration">
      <div class="form-field email-field">
        <label for="register-email">Email</label>
        <input
          id="register-email"
          v-model="email"
          type="email"
          maxlength="254"
          autocomplete="email"
          placeholder="andy@example.com"
          required
        >
      </div>

      <div class="form-field">
        <label for="register-password">密碼</label>
        <input
          id="register-password"
          v-model="password"
          type="password"
          minlength="8"
          maxlength="100"
          autocomplete="new-password"
          placeholder="至少 8 個字元"
          required
        >
      </div>

      <div class="form-field">
        <label for="register-password-confirmation">確認密碼</label>
        <input
          id="register-password-confirmation"
          v-model="passwordConfirmation"
          type="password"
          minlength="8"
          maxlength="100"
          autocomplete="new-password"
          placeholder="再次輸入密碼"
          required
        >
      </div>

      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? '註冊中…' : '建立會員' }}
      </button>
    </form>

    <p class="message" :class="{ success: isSuccess }" aria-live="polite">
      {{ message }}
    </p>
  </section>
</template>
