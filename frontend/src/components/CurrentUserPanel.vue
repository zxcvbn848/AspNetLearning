<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getCurrentUser } from '../services/authApi'
import type { UserResponse } from '../types/user'

const user = ref<UserResponse | null>(null)
const message = ref('讀取登入狀態中…')
const isLoading = ref(true)

/** 使用目前分頁保存的 JWT 讀取會員資料。 */
async function loadCurrentUser(): Promise<void> {
  const accessToken = window.sessionStorage.getItem('accessToken')

  if (!accessToken) {
    message.value = '尚未登入，請先使用上方登入表單取得 Token。'
    isLoading.value = false
    return
  }

  try {
    user.value = await getCurrentUser(accessToken)
    message.value = ''
  } catch (error) {
    // 無效或過期 Token 不應繼續留在瀏覽器中。
    window.sessionStorage.removeItem('accessToken')
    message.value = error instanceof Error ? error.message : '無法取得目前會員。'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadCurrentUser)
</script>

<template>
  <section class="api-section" aria-labelledby="current-user-heading">
    <div class="section-heading">
      <div>
        <p class="section-kicker">GET /auth/me</p>
        <h2 id="current-user-heading">目前登入會員</h2>
      </div>
      <span class="api-badge">Authorize</span>
    </div>

    <p v-if="isLoading || !user" class="auth-status" aria-live="polite">
      {{ message }}
    </p>

    <dl v-else class="user-details">
      <div>
        <dt>會員編號</dt>
        <dd>{{ user.id }}</dd>
      </div>
      <div>
        <dt>Email</dt>
        <dd>{{ user.email }}</dd>
      </div>
      <div>
        <dt>建立時間</dt>
        <dd>{{ new Date(user.createdAtUtc).toLocaleString('zh-TW') }}</dd>
      </div>
    </dl>
  </section>
</template>
