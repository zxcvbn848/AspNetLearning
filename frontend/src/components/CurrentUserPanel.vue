<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getCurrentUser, updateNickname } from '../services/authApi'
import type { UserResponse } from '../types/user'

const user = ref<UserResponse | null>(null)
const message = ref('讀取登入狀態中…')
const isLoading = ref(true)
const nickname = ref('')
const nicknameMessage = ref('')

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
    nickname.value = user.value.nickname ?? ''
    message.value = ''
  } catch (error) {
    // apiClient 會通知最上層元件統一清除失效的登入狀態。
    message.value = error instanceof Error ? error.message : '無法取得目前會員。'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadCurrentUser)

/** 已登入會員可以為舊帳號補上暱稱，或更新自己的暱稱。 */
async function saveNickname(): Promise<void> {
  const token = window.sessionStorage.getItem('accessToken')
  if (!token) return
  try {
    user.value = await updateNickname(token, nickname.value.trim())
    nickname.value = user.value.nickname ?? ''
    nicknameMessage.value = '暱稱已更新。'
  } catch (error) {
    nicknameMessage.value = error instanceof Error ? error.message : '暱稱更新失敗。'
  }
}
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
        <dt>暱稱</dt>
        <dd>{{ user.nickname || `會員 #${user.id}` }}</dd>
      </div>
      <div>
        <dt>建立時間</dt>
        <dd>{{ new Date(user.createdAtUtc).toLocaleString('zh-TW') }}</dd>
      </div>
    </dl>
    <form v-if="user" class="auth-form" @submit.prevent="saveNickname">
      <label for="profile-nickname">修改聊天室暱稱</label>
      <input id="profile-nickname" v-model="nickname" minlength="2" maxlength="30" required>
      <button type="submit">儲存暱稱</button>
      <p role="status">{{ nicknameMessage }}</p>
    </form>
  </section>
</template>
