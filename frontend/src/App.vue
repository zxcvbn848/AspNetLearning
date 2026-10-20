<script setup lang="ts">
// ref 建立響應式資料；資料改變時 Vue 會自動更新畫面。
import { nextTick, onMounted, ref } from 'vue'
import CurrentUserPanel from './components/CurrentUserPanel.vue'
import LoginForm from './components/LoginForm.vue'
import RegisterForm from './components/RegisterForm.vue'
import TodoListItem from './components/TodoListItem.vue'
import { createTodoItem, deleteTodoItem, getTodoItems, updateTodoItem } from './services/todoApi'
import type { TodoItem } from './types/todo'

const todos = ref<TodoItem[]>([])
const newTitle = ref('')
const message = ref('')
const isSuccess = ref(false)
const isCreating = ref(false)
const isConnected = ref(false)
const editDialog = ref<HTMLDialogElement | null>(null)
const editingTodo = ref<TodoItem | null>(null)
const editTitle = ref('')
const editError = ref('')
const isEditing = ref(false)
const authVersion = ref(0)
const hasAccessToken = ref(Boolean(window.sessionStorage.getItem('accessToken')))

/** 從 ASP.NET Core API 重新取得所有 Todo。 */
async function loadTodos(): Promise<void> {
  if (!window.sessionStorage.getItem('accessToken')) {
    todos.value = []
    hasAccessToken.value = false
    showMessage('登入後即可管理自己的待辦事項。')
    return
  }

  try {
    todos.value = await getTodoItems()
    hasAccessToken.value = true
    isConnected.value = true
  } catch (error) {
    if (error instanceof Error && error.message.includes('HTTP 401')) {
      window.sessionStorage.removeItem('accessToken')
      hasAccessToken.value = false
    }

    showError(error)
    isConnected.value = false
  }
}

/** 登入成功後重新載入會員區塊與該會員的 Todo。 */
async function handleLoggedIn(): Promise<void> {
  hasAccessToken.value = true
  authVersion.value++
  await loadTodos()
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

// 元件掛載到瀏覽器後，立即載入 PostgreSQL 中的 Todo。
onMounted(loadTodos)
</script>

<template>
  <main class="todo-app">
    <header class="page-header">
      <p class="eyebrow">Vue 3 × TypeScript × ASP.NET Core</p>
      <h1>API 整合平台</h1>
      <p class="subtitle">集中操作與驗證 AspNetLearning 專案的所有 API。</p>
    </header>

    <RegisterForm />
    <!-- key 改變時會重新建立元件，使登入成功後立即重新呼叫 GET /auth/me。 -->
    <LoginForm @logged-in="handleLoggedIn" />
    <CurrentUserPanel :key="authVersion" />

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

      <p v-else class="empty-state">目前沒有待辦事項。</p>
    </section>
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
