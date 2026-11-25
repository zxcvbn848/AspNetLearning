<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { HubConnectionBuilder, HubConnectionState, type HubConnection } from '@microsoft/signalr'
import { getRealtimeAccessToken } from '../services/apiClient'
import { getCurrentUser } from '../services/authApi'
import { filterDeletedChatMessages, loadChatGap, syncChatDeletions } from '../services/chatHistory'
import { useChatReadingState } from '../composables/useChatReadingState'

interface ChatMessagePreview {
  id: number
  userId: number
  nickname: string | null
  content: string
}

interface ChatMessage extends ChatMessagePreview {
  createdAtUtc: string
  replyTo: ChatMessagePreview | null
}

const messages = ref<ChatMessage[]>([])
const draft = ref('')
const replyTarget = ref<ChatMessagePreview | null>(null)
const draftInput = ref<HTMLInputElement | null>(null)
const status = ref<'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('disconnected')
const errorMessage = ref('')
const sending = ref(false)
const currentUserId = ref<number | null>(null)
const deleteTargetId = ref<number | null>(null)
const deleting = ref(false)
const loadingOlder = ref(false)
const hasMore = ref(false)
const messageList = ref<HTMLElement | null>(null)
const { isAtLatest, unreadCount, updatePosition, recordNewMessages, forgetMessage } = useChatReadingState()
const deletedIds = new Set<number>()
let connection: HubConnection | null = null
let disposed = false
let historyLoaded = false
let catchUpAfter: number | null = null
let historyVersion = 0
let olderBeforeId: number | null = null

/** 合併歷史與即時訊息；重連時同一則可能出現兩次，所以以 Id 去重。 */
async function addMessages(incoming: ChatMessage[], source: 'new' | 'recent' | 'older'): Promise<void> {
  const list = messageList.value
  const oldHeight = list?.scrollHeight ?? 0
  const oldTop = list?.scrollTop ?? 0
  if (list) updatePosition(list)
  const atBottom = isAtLatest.value
  const byId = new Map(messages.value.map(message => [message.id, message]))
  const visible = filterDeletedChatMessages(incoming, deletedIds)
  // 補收與廣播可能收到同一則訊息，沿用清單中的 Id 去重。
  if (source === 'new') {
    recordNewMessages(visible.map(message => message.id), byId)
  }
  for (const message of visible) byId.set(message.id, message)
  messages.value = [...byId.values()].sort((a, b) => a.id - b.id)
  await nextTick()
  if (!list) return
  // 往前補資料時保留閱讀位置；閱讀舊訊息時收到廣播也不強制捲到底。
  if (source === 'older') list.scrollTop = oldTop + list.scrollHeight - oldHeight
  else if (atBottom) list.scrollTop = list.scrollHeight
  updatePosition(list)
}

function removeMessages(ids: number[]): void {
  const missing = ids.filter(id => !deletedIds.has(id))
  if (missing.length === 0) return
  for (const id of missing) {
    deletedIds.add(id)
    forgetMessage(id)
  }
  messages.value = filterDeletedChatMessages(messages.value, deletedIds)
  if (replyTarget.value && deletedIds.has(replyTarget.value.id)) replyTarget.value = null
  if (deleteTargetId.value !== null && deletedIds.has(deleteTargetId.value)) deleteTargetId.value = null
  void nextTick(onMessageScroll)
}

async function loadCurrentUserId(): Promise<void> {
  const token = window.sessionStorage.getItem('accessToken')
  if (!token || currentUserId.value !== null) return
  try {
    const user = await getCurrentUser(token)
    if (!disposed) currentUserId.value = user.id
  } catch {
    // 暫時無法判斷作者時先隱藏刪除控制，重連再讀取。
  }
}

async function deleteMessage(id: number): Promise<void> {
  if (deleting.value || deleteTargetId.value !== id || connection?.state !== HubConnectionState.Connected) return
  deleting.value = true
  errorMessage.value = ''
  try {
    await connection.invoke('DeleteMessage', id)
    removeMessages([id])
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '訊息刪除失敗。'
  } finally {
    deleting.value = false
  }
}

/** 點擊按鈕或手動捲到底時，都清除本分頁的新訊息提示。 */
function returnToLatest(): void {
  const list = messageList.value
  if (!list) return
  list.scrollTop = list.scrollHeight
  updatePosition(list)
}

function onMessageScroll(): void {
  if (messageList.value) updatePosition(messageList.value)
}

function rememberCatchUpStart(): void {
  historyVersion++
  if (historyLoaded) catchUpAfter ??= messages.value.at(-1)?.id ?? 0
}

/** 初次讀取最新 50 則，重連時分批補收並核對刪除。 */
async function loadHistory(hub: HubConnection): Promise<void> {
  const version = ++historyVersion
  const isCurrent = () => !disposed && connection === hub && historyVersion === version
  const afterId = catchUpAfter
  try {
    const recent = await hub.invoke<ChatMessage[]>('GetRecentMessages')
    if (!isCurrent()) return
    if (afterId === null) {
      hasMore.value = recent.length === 50
      olderBeforeId = recent[0]?.id ?? null
      historyLoaded = true
      await addMessages(recent, 'recent')
      return
    }

    await loadChatGap(afterId, recent.at(-1)?.id ?? afterId,
      (cursor, throughId) => hub.invoke<ChatMessage[]>('GetNewerMessages', cursor, throughId),
      page => addMessages(page, 'new'), isCurrent)
    if (!isCurrent()) return
    await syncChatDeletions(messages.value,
      ids => hub.invoke<number[]>('GetMissingMessageIds', ids), removeMessages, isCurrent)
    if (isCurrent()) catchUpAfter = null
  } catch {
    if (!isCurrent()) return
    status.value = 'disconnected'
    errorMessage.value = '訊息載入失敗，請按「重新連線」再試一次。'
  }
}

/** 用上次查到的最舊 Id，向伺服器要求更早的一頁。 */
async function loadOlder(): Promise<void> {
  const hub = connection
  // 保留已查到的最舊 Id，就算這一頁全被刪除，仍能往前翻。
  const beforeId = olderBeforeId
  if (!hub || beforeId === null || loadingOlder.value || status.value !== 'connected') return
  loadingOlder.value = true
  errorMessage.value = ''
  try {
    const older = await hub.invoke<ChatMessage[]>('GetOlderMessages', beforeId)
    if (disposed || connection !== hub) return
    hasMore.value = older.length === 50
    olderBeforeId = older[0]?.id ?? beforeId
    await addMessages(older, 'older')
  } catch {
    errorMessage.value = '較早的訊息載入失敗，請再試一次。'
  } finally {
    loadingOlder.value = false
  }
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
  hub.on('MessageReceived', (message: ChatMessage) => { void addMessages([message], 'new') })
  hub.on('MessageDeleted', (id: number) => { removeMessages([id]) })
  hub.onreconnecting(() => {
    rememberCatchUpStart()
    status.value = 'reconnecting'
  })
  hub.onreconnected(() => {
    status.value = 'connected'
    void loadCurrentUserId()
    void loadHistory(hub)
  })
  hub.onclose(() => {
    if (disposed || connection !== hub) return
    rememberCatchUpStart()
    status.value = 'disconnected'
  })

  try {
    await hub.start()
    if (disposed) { await hub.stop(); return }
    status.value = 'connected'
    void loadCurrentUserId()
    await loadHistory(hub)
  } catch {
    if (!disposed) {
      status.value = 'disconnected'
      errorMessage.value = '聊天室連線失敗，請確認登入狀態後重試。'
    }
  }
}

function selectReply(message: ChatMessagePreview): void {
  replyTarget.value = message
  draftInput.value?.focus()
}

/** 一般訊息與回覆都等廣播回來後才加入清單。 */
async function sendMessage(): Promise<void> {
  const text = draft.value.trim()
  if (sending.value || !text || text.length > 500 || !connection || connection.state !== HubConnectionState.Connected) return
  sending.value = true
  errorMessage.value = ''
  try {
    if (replyTarget.value) {
      await connection.invoke('SendReply', text, replyTarget.value.id)
    } else {
      await connection.invoke('SendMessage', text)
    }
    draft.value = ''
    replyTarget.value = null
  } catch (error) {
    const message = error instanceof Error ? error.message : '訊息傳送失敗。'
    errorMessage.value = message.endsWith('CHAT_RATE_LIMIT') ? '傳送太頻繁，請稍後再試。' : message
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
      <p>先顯示最近 50 則，可分批載入更早訊息。</p>
    </div>

    <p role="status" class="chat-status">
      連線狀態：{{ status === 'connected' ? '已連線' : status === 'reconnecting' ? '重新連線中' : status === 'connecting' ? '連線中' : '未連線' }}
      <button v-if="status === 'disconnected'" type="button" class="secondary-button" @click="connect">重新連線</button>
    </p>

    <button v-if="hasMore" type="button" class="secondary-button" :disabled="loadingOlder || status !== 'connected'" @click="loadOlder">
      {{ loadingOlder ? '載入中…' : '載入更早訊息' }}
    </button>
    <p v-else-if="messages.length" role="status">已載入所有歷史訊息。</p>
    <ol ref="messageList" class="chat-messages" role="log" aria-live="polite" aria-label="聊天室訊息" @scroll="onMessageScroll">
      <li v-for="message in messages" :key="message.id" class="chat-message"
        :class="{ 'chat-message-own': message.userId === currentUserId }">
        <div class="chat-meta">
          <strong>
            {{ message.nickname || `會員 #${message.userId}` }}
            <span v-if="message.userId === currentUserId" class="chat-self-badge">你</span>
          </strong>
          <time :datetime="message.createdAtUtc">{{ new Date(message.createdAtUtc).toLocaleString('zh-TW') }}</time>
        </div>
        <blockquote v-if="message.replyTo" class="chat-quote">
          <strong>{{ message.replyTo.nickname || `會員 #${message.replyTo.userId}` }}</strong>
          <p>{{ message.replyTo.content }}</p>
        </blockquote>
        <p>{{ message.content }}</p>
        <button type="button" class="secondary-button chat-reply-button"
          :disabled="status !== 'connected' || sending" @click="selectReply(message)">回覆</button>
        <template v-if="message.userId === currentUserId">
          <button v-if="deleteTargetId !== message.id" type="button" class="secondary-button chat-reply-button"
            :disabled="status !== 'connected' || deleting" @click="deleteTargetId = message.id">刪除</button>
          <template v-else>
            <button type="button" class="secondary-button chat-reply-button"
              :disabled="status !== 'connected' || deleting" @click="deleteMessage(message.id)">確認刪除</button>
            <button type="button" class="secondary-button chat-reply-button"
              :disabled="deleting" @click="deleteTargetId = null">取消</button>
          </template>
        </template>
      </li>
    </ol>
    <div class="chat-reading-status">
      <span role="status" aria-live="polite">{{ unreadCount ? `有 ${unreadCount} 則新訊息` : '' }}</span>
      <button v-if="!isAtLatest" type="button" class="secondary-button" @click="returnToLatest">回到最新 ↓</button>
    </div>
    <p v-if="!messages.length" class="empty-state">還沒有訊息，傳送第一則吧。</p>

    <form class="chat-form" @submit.prevent="sendMessage">
      <label for="chat-draft">新訊息</label>
      <div v-if="replyTarget" class="chat-quote">
        <strong>回覆 {{ replyTarget.nickname || `會員 #${replyTarget.userId}` }}</strong>
        <p>{{ replyTarget.content }}</p>
        <button type="button" class="secondary-button chat-reply-button" :disabled="sending" @click="replyTarget = null">取消回覆</button>
      </div>
      <div class="input-row">
        <input id="chat-draft" ref="draftInput" v-model="draft" maxlength="500" autocomplete="off" placeholder="輸入訊息（最多 500 字）" :disabled="status !== 'connected'" :readonly="sending">
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
.chat-message {
  width: fit-content;
  max-width: min(92%, 44rem);
  margin-block: 8px;
  padding: 10px 12px;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--background);
  overflow-wrap: anywhere;
}
.chat-message-own {
  margin-inline-start: auto;
  border-color: var(--primary);
  background: var(--highlight);
}
.chat-reading-status { display: flex; justify-content: space-between; align-items: center; gap: 12px; min-height: 44px; color: var(--muted); }
.chat-reading-status button { padding: 8px 12px; color: var(--text); background: var(--secondary); }
.chat-message p { margin: 6px 0 0; white-space: pre-wrap; }
.chat-quote { margin: 8px 0 0; padding: 8px 12px; border-inline-start: 3px solid var(--border); color: var(--muted); overflow-wrap: anywhere; }
.chat-quote p { margin: 4px 0 0; white-space: pre-wrap; }
.chat-message .chat-reply-button, .chat-form .chat-reply-button { margin-top: 6px; padding: 4px 10px; color: var(--text); background: var(--secondary); }
.chat-meta { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 2px 10px; color: var(--muted); font-size: .85rem; }
.chat-self-badge { display: inline-block; margin-inline-start: .35em; font-weight: 700; color: var(--primary); }
.chat-form { display: grid; gap: 10px; margin-top: 20px; }
.chat-form label { font-weight: 700; }
.chat-form .input-row { display: flex; gap: 10px; }
.chat-form input { flex: 1; min-width: 0; padding: 12px; border: 1px solid var(--border); border-radius: 10px; color: var(--text); background: var(--field); }
.chat-form button { padding: 10px 18px; color: var(--button-text); background: var(--button-background); }
.chat-error { min-height: 1.4em; margin: 0; color: var(--danger); }
@media (max-width: 600px) { .chat-form .input-row { flex-direction: column; } }
</style>
