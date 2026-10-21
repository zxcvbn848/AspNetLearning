<script setup lang="ts">
// ref 建立響應式資料；資料改變時 Vue 會自動更新畫面。
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import CurrentUserPanel from './components/CurrentUserPanel.vue'
import ErrorHandlingPanel from './components/ErrorHandlingPanel.vue'
import LoginForm from './components/LoginForm.vue'
import RegisterForm from './components/RegisterForm.vue'
import TodoListItem from './components/TodoListItem.vue'
import { logoutUser } from './services/authApi'
import { authUnauthorizedEvent } from './services/apiClient'
import { createTodoItem, deleteTodoItem, getTodoItems, updateTodoItem } from './services/todoApi'
import { connectTodoUpdates } from './services/todoRealtime'
import type { HubConnection } from '@microsoft/signalr'
import type { TodoItem } from './types/todo'

// 使用字串聯集限制頁面名稱，避免傳入拼錯或不存在的頁面。
type DashboardPage = 'member' | 'todos' | 'errors' | 'status'
type AuthForm = 'login' | 'register'

// 導覽項目的資料結構，讓按鈕可以用同一份陣列產生。
interface NavigationItem {
  id: DashboardPage
  label: string
  description: string
}

// 導覽設定只負責顯示文字與對應頁面，不包含 API 邏輯。
const navigationItems: NavigationItem[] = [
  { id: 'member', label: '會員', description: '註冊、登入與會員資料' },
  { id: 'todos', label: '待辦事項', description: '管理自己的 Todo' },
  { id: 'errors', label: '錯誤測試', description: 'Problem Details' },
  { id: 'status', label: 'API 狀態', description: '連線與登入狀態' },
]

// ref 保存目前選取的頁面；變更 value 時，Vue 會自動切換畫面。
const activePage = ref<DashboardPage>('member')
// 未登入時只顯示一張會員表單，避免登入與註冊表單同時拉長頁面。
const activeAuthForm = ref<AuthForm>('login')
const todos = ref<TodoItem[]>([])
const newTitle = ref('')
const message = ref('')
const isSuccess = ref(false)
const isCreating = ref(false)
const isLoggingOut = ref(false)
const isConnected = ref(false)
const realtimeState = ref<'connected' | 'reconnecting' | 'disconnected'>('disconnected')
let realtimeConnection: HubConnection | null = null
const editDialog = ref<HTMLDialogElement | null>(null)
const editingTodo = ref<TodoItem | null>(null)
const editTitle = ref('')
const editError = ref('')
const isEditing = ref(false)
const hasAccessToken = ref(Boolean(window.sessionStorage.getItem('accessToken')))
// Vite 正式建置時 DEV 是 false，因此不會顯示刻意產生錯誤的學習區塊。
const isDevelopment = import.meta.env.DEV
// Vite 讀取容器環境變數；關閉時不建立 SignalR 連線，畫面也明確標示停用。
const realtimeEnabled = import.meta.env.VITE_TODO_REALTIME_ENABLED !== 'false'
// localStorage 會在關閉瀏覽器後保留選擇；未選過時維持原本的亮色模式。
const isDark = ref(window.localStorage.getItem('aspnetlearning-theme') === 'dark')
// data-theme 讓 CSS 根據同一個屬性切換整頁的色彩變數。
document.documentElement.dataset.theme = isDark.value ? 'dark' : 'light'

/** 切換模式並保存選擇，重新整理頁面後仍沿用。 */
function toggleTheme(): void {
  isDark.value = !isDark.value
  const theme = isDark.value ? 'dark' : 'light'
  document.documentElement.dataset.theme = theme
  window.localStorage.setItem('aspnetlearning-theme', theme)
}

/** 切換功能頁；進入待辦頁時同步取得最新資料。 */
function selectPage(page: DashboardPage): void {
  activePage.value = page

  if (page === 'todos') {
    void loadTodos()
  }
}

/** 從 ASP.NET Core API 重新取得所有 Todo。 */
async function loadTodos(): Promise<void> {
  const accessToken = window.sessionStorage.getItem('accessToken')
  if (!accessToken) {
    todos.value = []
    hasAccessToken.value = false
    showMessage('登入後即可管理自己的待辦事項。')
    return
  }

  try {
    const items = await getTodoItems()
    // 回應抵達前若已登出或換帳號，就捨棄過期資料。
    if (window.sessionStorage.getItem('accessToken') !== accessToken) return
    todos.value = items
    hasAccessToken.value = true
    isConnected.value = true
  } catch (error) {
    if (window.sessionStorage.getItem('accessToken') !== accessToken) return
    showError(error)
    isConnected.value = false
  }
}

/** 登入成功後重新載入會員區塊與該會員的 Todo。 */
async function handleLoggedIn(): Promise<void> {
  hasAccessToken.value = true
  showMessage('')
  await loadTodos()
  await startRealtime()
}

/** 每個已登入分頁各自連線；換帳號或登出時停止舊連線。 */
async function startRealtime(): Promise<void> {
  await stopRealtime()
  if (!realtimeEnabled || !window.sessionStorage.getItem('accessToken')) return

  const connection = connectTodoUpdates(
    () => { void loadTodos() },
    state => { if (realtimeConnection === connection) realtimeState.value = state },
  )
  realtimeConnection = connection
  try {
    await connection.start()
    if (realtimeConnection === connection) realtimeState.value = 'connected'
  } catch {
    if (realtimeConnection === connection) realtimeState.value = 'disconnected'
  }
}

async function stopRealtime(): Promise<void> {
  const connection = realtimeConnection
  realtimeConnection = null
  realtimeState.value = 'disconnected'
  await connection?.stop()
}

/** 清除目前分頁的登入資料，並讓會員與 Todo 畫面回到未登入狀態。 */
function clearAuthState(message: string, success: boolean): void {
  void stopRealtime()
  window.sessionStorage.removeItem('accessToken')
  hasAccessToken.value = false
  isConnected.value = false
  todos.value = []
  newTitle.value = ''
  editDialog.value?.close()
  activePage.value = 'member'
  activeAuthForm.value = 'login'
  showMessage(message, success)
}

/** 先撤銷伺服器工作階段，成功後才清掉目前畫面的登入資料。 */
async function handleLoggedOut(): Promise<void> {
  if (isLoggingOut.value) return
  isLoggingOut.value = true
  try {
    await logoutUser()
    clearAuthState('已登出。', true)
  } catch (error) {
    showError(error)
  } finally {
    isLoggingOut.value = false
  }
}

/** 受保護 API 回傳 401 時，使用與手動登出相同的清理流程。 */
function handleUnauthorized(): void {
  if (!hasAccessToken.value) {
    return
  }

  clearAuthState('登入已失效，請重新登入。', false)
}

/** 呼叫 POST /todos 建立待辦事項。 */
async function createTodo(): Promise<void> {
  const title = newTitle.value.trim()

  if (!title) {
    showMessage('請輸入待辦事項。')
    return
  }

  isCreating.value = true
  showMessage('')

  try {
    await createTodoItem(title)
    newTitle.value = ''
    showMessage('待辦事項已新增。', true)
    await loadTodos()
  } catch (error) {
    showError(error)
  } finally {
    isCreating.value = false
  }
}

/** 呼叫 PUT /todos/{id} 切換完成狀態。 */
async function toggleTodo(todo: TodoItem): Promise<void> {
  try {
    await updateTodoItem(todo.id, todo.title, !todo.isCompleted)
    showMessage('完成狀態已更新。', true)
    await loadTodos()
  } catch (error) {
    showError(error)
  }
}

/** 將待編輯資料放進表單，並開啟頁面內的自訂對話框。 */
async function editTodo(todo: TodoItem): Promise<void> {
  editingTodo.value = todo
  editTitle.value = todo.title
  editError.value = ''

  // 等 Vue 建立 dialog 元素後，才呼叫瀏覽器的對話框 API。
  await nextTick()
  editDialog.value?.showModal()
}

/** 驗證表單後，呼叫 PUT /todos/{id} 更新資料。 */
async function saveTodoEdit(): Promise<void> {
  const todo = editingTodo.value
  const title = editTitle.value.trim()

  if (!todo) {
    return
  }

  if (!title) {
    editError.value = '請輸入待辦事項。'
    return
  }

  if (title === todo.title) {
    editDialog.value?.close()
    return
  }

  isEditing.value = true
  editError.value = ''

  try {
    await updateTodoItem(todo.id, title, todo.isCompleted)
    editDialog.value?.close()
    showMessage('待辦事項已更新。', true)
    await loadTodos()
  } catch (error) {
    editError.value = error instanceof Error ? error.message : '發生未預期的錯誤。'
  } finally {
    isEditing.value = false
  }
}

/** 關閉編輯視窗；close 事件會清除暫存資料。 */
function cancelEdit(): void {
  editDialog.value?.close()
}

/** 對話框關閉後，清除上一筆編輯內容。 */
function resetEdit(): void {
  editingTodo.value = null
  editTitle.value = ''
  editError.value = ''
}

/** 使用者確認後，呼叫 DELETE /todos/{id}。 */
async function removeTodo(todo: TodoItem): Promise<void> {
  if (!window.confirm(`確定要刪除「${todo.title}」嗎？`)) {
    return
  }

  try {
    await deleteTodoItem(todo.id)
    showMessage('待辦事項已刪除。', true)
    await loadTodos()
  } catch (error) {
    showError(error)
  }
}

/** 更新畫面訊息與成功狀態。 */
function showMessage(text: string, success = false): void {
  message.value = text
  isSuccess.value = success
}

/** 將 unknown 型別的 catch 值安全轉成可顯示文字。 */
function showError(error: unknown): void {
  showMessage(error instanceof Error ? error.message : '發生未預期的錯誤。')
}

// 元件掛載後監聽授權失效事件，並立即載入 PostgreSQL 中的 Todo。
onMounted(() => {
  window.addEventListener(authUnauthorizedEvent, handleUnauthorized)
  if (hasAccessToken.value) {
    void loadTodos()
    void startRealtime()
  }
})

// 元件移除前解除全域事件，避免重複註冊造成同一事件執行多次。
onBeforeUnmount(() => {
  window.removeEventListener(authUnauthorizedEvent, handleUnauthorized)
  void stopRealtime()
})
</script>

<template>
  <main class="dashboard-shell">
    <header class="page-header">
      <div class="header-top">
        <p class="eyebrow">Vue 3 × TypeScript × ASP.NET Core</p>
        <!-- aria-pressed 告訴輔助工具「暗色模式」目前是否啟用。 -->
        <button type="button" class="theme-toggle" :aria-pressed="isDark" @click="toggleTheme">
          <span aria-hidden="true">{{ isDark ? '☀' : '☾' }}</span>
          暗色模式
        </button>
      </div>
      <h1>API 整合平台</h1>
      <p class="subtitle">集中操作與驗證 AspNetLearning 專案的所有 API。</p>
    </header>

    <div class="dashboard-layout">
      <!-- nav 表示這組按鈕是主要功能導覽，方便輔助工具辨識。 -->
      <nav class="dashboard-nav" aria-label="API 功能導覽">
        <!-- v-for 依 navigationItems 建立相同結構的導覽按鈕。 -->
        <button
          v-for="item in navigationItems"
          :key="item.id"
          type="button"
          class="nav-button"
          :class="{ active: activePage === item.id }"
          :aria-current="activePage === item.id ? 'page' : undefined"
          @click="selectPage(item.id)"
        >
          <span>{{ item.label }}</span>
          <small>{{ item.description }}</small>
        </button>

        <button v-if="hasAccessToken" type="button" class="logout-button" :disabled="isLoggingOut" @click="handleLoggedOut()">
          {{ isLoggingOut ? '登出中…' : '登出' }}
        </button>
      </nav>

      <div class="dashboard-content">
        <p v-if="activePage !== 'todos' && message" class="message" :class="{ success: isSuccess }" role="status">
          {{ message }}
        </p>
        <!-- v-if 只建立目前選取的功能頁，避免所有表單同時拉長畫面。 -->
        <section v-if="activePage === 'member'" aria-labelledby="member-page-heading">
          <div class="content-heading">
            <p class="eyebrow">Member API</p>
            <h2 id="member-page-heading">會員</h2>
            <p>建立帳號、取得 JWT，並讀取目前登入會員。</p>
          </div>

          <template v-if="!hasAccessToken">
            <div class="auth-switch" aria-label="會員操作">
              <button
                type="button"
                :class="{ active: activeAuthForm === 'login' }"
                @click="activeAuthForm = 'login'"
              >
                登入
              </button>
              <button
                type="button"
                :class="{ active: activeAuthForm === 'register' }"
                @click="activeAuthForm = 'register'"
              >
                註冊
              </button>
            </div>

            <!-- v-if 只建立目前選擇的表單，切換時會清除尚未送出的輸入內容。 -->
            <LoginForm
              v-if="activeAuthForm === 'login'"
              @logged-in="handleLoggedIn"
            />
            <RegisterForm v-else />
          </template>

          <!-- 登入後只顯示會員資料，登出按鈕位於左側導覽列。 -->
          <CurrentUserPanel v-else />
        </section>

        <section v-else-if="activePage === 'todos'" aria-labelledby="todo-page-heading">
          <div class="content-heading">
            <p class="eyebrow">Todo API</p>
            <h2 id="todo-page-heading">待辦事項</h2>
            <p>新增、完成、修改與刪除目前會員的待辦事項。開兩個分頁可觀察即時同步。</p>
            <p v-if="hasAccessToken" role="status">即時連線：{{ !realtimeEnabled ? '已停用' : realtimeState === 'connected' ? '已連線' : realtimeState === 'reconnecting' ? '重新連線中' : '未連線' }}</p>
          </div>

          <!-- prevent 阻止瀏覽器送出表單後重新整理整頁。 -->
          <form class="create-form api-section" @submit.prevent="createTodo">
            <p class="section-kicker">POST /todos</p>
            <label for="new-title">新增待辦事項</label>
            <div class="input-row">
              <!-- v-model 讓輸入框與 newTitle 保持雙向同步。 -->
              <input
                id="new-title"
                v-model="newTitle"
                maxlength="100"
                placeholder="例如：閱讀 Vue 文件"
                autocomplete="off"
                :disabled="!hasAccessToken"
                required
              >
              <button type="submit" :disabled="!hasAccessToken || isCreating">
                {{ isCreating ? '新增中…' : '新增' }}
              </button>
            </div>
          </form>

          <p class="message" :class="{ success: isSuccess }" aria-live="polite">
            {{ message }}
          </p>

          <section class="todo-section" aria-labelledby="todo-heading">
            <div class="section-heading">
              <h2 id="todo-heading">待辦事項</h2>
              <span class="todo-count">{{ todos.length }} 筆</span>
            </div>

            <ul v-if="todos.length" class="todo-list">
              <!-- v-for 根據陣列建立元件；key 讓 Vue 使用 Id 識別每一列。 -->
              <TodoListItem
                v-for="todo in todos"
                :key="todo.id"
                :todo="todo"
                @toggle="toggleTodo"
                @edit="editTodo"
                @remove="removeTodo"
              />
            </ul>

            <p v-else class="empty-state">
              {{ hasAccessToken ? '目前沒有待辦事項。' : '請先到會員頁登入。' }}
            </p>
          </section>
        </section>

        <!-- 例外測試端點只存在於開發環境；正式建置改顯示說明文字。 -->
        <ErrorHandlingPanel v-else-if="activePage === 'errors' && isDevelopment" />
        <section v-else-if="activePage === 'errors'" class="empty-page">
          <h2>錯誤測試</h2>
          <p>正式環境不提供刻意產生例外的測試功能。</p>
        </section>

        <section v-else class="status-page" aria-labelledby="status-page-heading">
          <div class="content-heading">
            <p class="eyebrow">Runtime Status</p>
            <h2 id="status-page-heading">API 狀態</h2>
            <p>查看目前瀏覽器分頁保存的登入與連線狀態。</p>
          </div>

          <dl class="status-list">
            <div>
              <dt>登入狀態</dt>
              <dd>{{ hasAccessToken ? '已登入' : '未登入' }}</dd>
            </div>
            <div>
              <dt>Todo API</dt>
              <dd>{{ !hasAccessToken ? '登入後檢查' : isConnected ? '連線正常' : '無法連線' }}</dd>
            </div>
            <div>
              <dt>SignalR 即時通知</dt>
              <dd>{{ !realtimeEnabled ? '已停用' : !hasAccessToken ? '登入後連線' : realtimeState === 'connected' ? '已連線' : realtimeState === 'reconnecting' ? '重新連線中' : '未連線' }}</dd>
            </div>
            <div>
              <dt>前端環境</dt>
              <dd>{{ isDevelopment ? 'Development' : 'Production' }}</dd>
            </div>
          </dl>
        </section>
      </div>
    </div>
  </main>

  <!-- dialog 提供鍵盤焦點管理；內容與樣式由 Vue 頁面自行控制。 -->
  <dialog ref="editDialog" class="edit-dialog" @close="resetEdit" @cancel.prevent="cancelEdit">
    <form class="edit-form" @submit.prevent="saveTodoEdit">
      <div>
        <p class="dialog-eyebrow">編輯待辦事項</p>
        <h2>修改內容</h2>
      </div>

      <label for="edit-title">待辦事項標題</label>
      <input
        id="edit-title"
        v-model="editTitle"
        maxlength="100"
        autocomplete="off"
        autofocus
        required
      >
      <p class="edit-hint">最多 100 個字元</p>
      <p class="edit-error" aria-live="polite">{{ editError }}</p>

      <div class="dialog-actions">
        <button type="button" class="secondary-button" :disabled="isEditing" @click="cancelEdit">
          取消
        </button>
        <button type="submit" class="primary-button" :disabled="isEditing">
          {{ isEditing ? '儲存中…' : '儲存' }}
        </button>
      </div>
    </form>
  </dialog>

  <footer>
    <span class="status-dot" :class="{ connected: isConnected, pending: !hasAccessToken }">
      {{ !hasAccessToken ? '登入後載入 Todo API' : isConnected ? 'API 與 PostgreSQL 連線正常' : '無法連線 API' }}
    </span>
  </footer>
</template>
