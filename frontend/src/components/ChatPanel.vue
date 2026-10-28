<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { getRealtimeAccessToken } from '../services/apiClient'

interface ChatMessage {
  id: number
  userId: number
  content: string
  createdAtUtc: string
}

const messages = ref<ChatMessage[]>([])
const draft = ref('')
const status = ref<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('disconnected')
const errorMessage = ref('')
const sending = ref(false)
const messageList = ref<HTMLElement | null>(null)
let connection: HubConnection | null = null
let disposed = false

/** 合併歷史與即時訊息；重連時同一則可能出現兩次，所以以 Id 去重。 */
async function addMessages(incoming: ChatMessage[]): Promise<void> {
  const byId = new Map(messages.value.map(message => [message.id, message]))
  for (const message of incoming) byId.set(message.id, message)
  messages.value = [...byId.values()].sort((a, b) => a.id - b.id).slice(-50)
  await nextTick()
  messageList.value?.scrollTo({ top: messageList.value.scrollHeight })
}

/** 進入聊天室與重連後，從資料庫補回最近 50 則訊息。 */
async function loadHistory(hub: HubConnection): Promise<void> {
  const recent = await hub.invoke<ChatMessage[]>('GetRecentMessages')
  if (connection === hub && !disposed) await addMessages(recent)
}

/** 建立持續連線；意外斷線時由 SignalR 自動嘗試重連。 */
async function connect(): Promise<void> {
  if (status.value === 'connecting' || status.value === 'reconnecting') return
  status.value = 'connecting'
  errorMessage.value = ''
  await connection?.stop()

  const hub = new HubConnectionBuilder()
    .withUrl('/hubs/chat', { accessTokenFactory: getRealtimeAccessToken })
    .withAutomaticReconnect()
    .build()
  connection = hub
  hub.on('MessageReceived', (message: ChatMessage) => { void addMessages([message]) })
  hub.onreconnecting(() => { status.value = 'reconnecting' })
  hub.onreconnected(() => {
    status.value = 'connected'
    void loadHistory(hub).catch(() => {
      errorMessage.value = '歷史訊息載入失敗，請重新連線。'
    })
  })
  hub.onclose(() => { if (!disposed && connection === hub) status.value = 'disconnected' })

  try {
    await hub.start()
    if (disposed) { await hub.stop(); return }
    status.value = 'connected'
    await loadHistory(hub)
  } catch {
    if (!disposed) {
      status.value = 'disconnected'
      errorMessage.value = '聊天室連線失敗，請確認登入狀態後重試。'
    }
  }
}

/** Hub 的 SendMessage 方法負責驗證、儲存與廣播；畫面等廣播回來後再加入清單。 */
async function sendMessage(): Promise<void> {
  const text = draft.value.trim()
  if (!text || text.length > 500 || !connection || connection.state !== HubConnectionState.Connected) return
  sending.value = true
  errorMessage.value = ''
  try {
    await connection.invoke('SendMessage', text)
    draft.value = ''
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '訊息傳送失敗。'
  } finally {
    sending.value = false
  }
}

onMounted(() => { void connect() })
onBeforeUnmount(() => {
  disposed = true
  void connection?.stop()
})
</script>

<template>
  <section aria-labelledby="chat-heading">
    <div class="content-heading">
      <p class="eyebrow">SignalR /hubs/chat</p>
      <h2 id="chat-heading">公開聊天室</h2>
      <p>登入會員都能看到這裡的訊息；畫面顯示最近 50 則。</p>
    </div>

    <p role="status" class="chat-status">
      連線狀態：{{ status === 'connected' ? '已連線' : status === 'reconnecting' ? '重新連線中' : status === 'connecting' ? '連線中' : '未連線' }}
      <button v-if="status === 'disconnected'" type="button" class="secondary-button" @click="connect">重新連線</button>
    </p>

    <ol ref="messageList" class="chat-messages" role="log" aria-live="polite" aria-label="聊天室訊息">
      <li v-for="message in messages" :key="message.id" class="chat-message">
        <div class="chat-meta">
          <strong>會員 #{{ message.userId }}</strong>
          <time :datetime="message.createdAtUtc">{{ new Date(message.createdAtUtc).toLocaleString('zh-TW') }}</time>
        </div>
        <p>{{ message.content }}</p>
      </li>
    </ol>
    <p v-if="!messages.length" class="empty-state">還沒有訊息，傳送第一則吧。</p>

    <form class="chat-form" @submit.prevent="sendMessage">
      <label for="chat-draft">新訊息</label>
      <div class="input-row">
        <input id="chat-draft" v-model="draft" maxlength="500" autocomplete="off" placeholder="輸入訊息（最多 500 字）" :disabled="status !== 'connected'">
        <button type="submit" :disabled="status !== 'connected' || sending || !draft.trim()">{{ sending ? '傳送中…' : '傳送' }}</button>
      </div>
      <p class="chat-error" role="alert">{{ errorMessage }}</p>
    </form>
  </section>
</template>

<style scoped>
.chat-status { display: flex; align-items: center; gap: 12px; margin: 20px 0; color: var(--muted); }
.chat-status button { padding: 8px 12px; color: var(--text); background: var(--secondary); }
.chat-messages { height: 380px; overflow-y: auto; margin: 0; padding: 12px; list-style: none; border: 1px solid var(--border); border-radius: 12px; background: var(--surface); }
.chat-message { padding: 10px 4px; overflow-wrap: anywhere; }
.chat-message + .chat-message { border-top: 1px solid var(--border); }
.chat-message p { margin: 6px 0 0; white-space: pre-wrap; }
.chat-meta { display: flex; justify-content: space-between; gap: 10px; color: var(--muted); font-size: .85rem; }
.chat-form { display: grid; gap: 10px; margin-top: 20px; }
.chat-form label { font-weight: 700; }
.chat-form .input-row { display: flex; gap: 10px; }
.chat-form input { flex: 1; min-width: 0; padding: 12px; border: 1px solid var(--border); border-radius: 10px; color: var(--text); background: var(--field); }
.chat-form button { padding: 10px 18px; color: var(--button-text); background: var(--button-background); }
.chat-error { min-height: 1.4em; margin: 0; color: var(--danger); }
@media (max-width: 600px) { .chat-form .input-row { flex-direction: column; } }
</style>
