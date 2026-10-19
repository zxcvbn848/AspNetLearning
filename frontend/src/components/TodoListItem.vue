<script setup lang="ts">
import type { TodoItem } from '../types/todo'

// defineProps 宣告父元件必須傳入一筆 TodoItem。
defineProps<{ todo: TodoItem }>()

// defineEmits 宣告元件可以通知父元件的事件與資料型別。
defineEmits<{
  toggle: [todo: TodoItem]
  edit: [todo: TodoItem]
  remove: [todo: TodoItem]
}>()
</script>

<template>
  <li class="todo-item" :class="{ completed: todo.isCompleted }">
    <input
      type="checkbox"
      :checked="todo.isCompleted"
      :aria-label="`切換「${todo.title}」的完成狀態`"
      @change="$emit('toggle', todo)"
    >

    <!-- Vue 的文字插值會自動跳脫 HTML，避免輸入內容被當成程式執行。 -->
    <span class="todo-title">{{ todo.title }}</span>

    <div class="todo-actions">
      <button type="button" class="action-button" @click="$emit('edit', todo)">編輯</button>
      <button type="button" class="action-button delete" @click="$emit('remove', todo)">刪除</button>
    </div>
  </li>
</template>
