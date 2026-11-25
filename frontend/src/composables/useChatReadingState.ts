import { computed, ref } from 'vue'

/** 管理目前分頁的閱讀位置；不代表其他裝置的已讀狀態。 */
export function useChatReadingState() {
  const isAtLatest = ref(true)
  const unreadIds = ref(new Set<number>())
  const unreadCount = computed(() => unreadIds.value.size)

  function markRead(): void {
    unreadIds.value.clear()
  }

  function updatePosition(list: { scrollHeight: number; scrollTop: number; clientHeight: number }): void {
    // 與既有自動捲動使用相同的 40px 容許範圍。
    isAtLatest.value = list.scrollHeight - list.scrollTop - list.clientHeight < 40
    if (isAtLatest.value) markRead()
  }

  function recordNewMessages(ids: number[], existing: ReadonlyMap<number, unknown>): void {
    if (isAtLatest.value) return
    for (const id of ids) {
      if (!existing.has(id)) unreadIds.value.add(id)
    }
  }

  function forgetMessage(id: number): void {
    unreadIds.value.delete(id)
  }

  return { isAtLatest, unreadCount, updatePosition, recordNewMessages, forgetMessage }
}
