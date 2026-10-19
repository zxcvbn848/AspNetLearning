<script setup lang="ts">
// ref 建立響應式資料；資料改變時 Vue 會自動更新畫面。
import { onMounted, ref } from 'vue'
import TodoListItem from './components/TodoListItem.vue'
import { createTodoItem, deleteTodoItem, getTodoItems, updateTodoItem } from './services/todoApi'
import type { TodoItem } from './types/todo'

const todos = ref<TodoItem[]>([])
const newTitle = ref('')
const message = ref('')
const isSuccess = ref(false)
const isCreating = ref(false)
const isConnected = ref(false)

/** 從 ASP.NET Core API 重新取得所有 Todo。 */
async function loadTodos(): Promise<void> {
  try {
    todos.value = await getTodoItems()
    isConnected.value = true
  } catch (error) {
    showError(error)
    isConnected.value = false
  }
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

/** 取得新標題後，呼叫 PUT /todos/{id} 更新資料。 */
async function editTodo(todo: TodoItem): Promise<void> {
  const title = window.prompt('修改待辦事項（最多 100 個字元）', todo.title)?.trim()

  if (!title || title === todo.title) {
    return
  }

  if (title.length > 100) {
    showMessage('Title 最多只能有 100 個字元。')
    return
  }

  try {
    await updateTodoItem(todo.id, title, todo.isCompleted)
    showMessage('待辦事項已更新。', true)
    await loadTodos()
  } catch (error) {
    showError(error)
  }
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
      <h1>我的待辦清單</h1>
      <p class="subtitle">Vue 前端透過 Todo API 讀寫 PostgreSQL。</p>
    </header>

    <!-- prevent 阻止瀏覽器送出表單後重新整理整頁。 -->
    <form class="create-form" @submit.prevent="createTodo">
      <label for="new-title">新增待辦事項</label>
      <div class="input-row">
        <!-- v-model 讓輸入框與 newTitle 保持雙向同步。 -->
        <input
          id="new-title"
          v-model="newTitle"
          maxlength="100"
          placeholder="例如：閱讀 Vue 文件"
          autocomplete="off"
          required
        >
        <button type="submit" :disabled="isCreating">
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

  <footer>
    <span class="status-dot" :class="isConnected ? 'connected' : 'disconnected'">
      {{ isConnected ? 'API 與 PostgreSQL 連線正常' : '無法連線 API' }}
    </span>
  </footer>
</template>
