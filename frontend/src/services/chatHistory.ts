/** 補收固定範圍；即時廣播不會推進查詢游標。 */
export async function loadChatGap<T extends { id: number }>(
  afterId: number,
  throughId: number,
  fetchPage: (afterId: number, throughId: number) => Promise<T[]>,
  appendPage: (page: T[]) => Promise<void>,
  isCurrent: () => boolean,
): Promise<void> {
  while (afterId < throughId && isCurrent()) {
    const page = await fetchPage(afterId, throughId)
    if (!isCurrent() || page.length === 0) return
    await appendPage(page)
    afterId = page[page.length - 1]!.id
  }
}
/** 移除已刪除的訊息及其引用，也適用於較晚抵達的查詢結果。 */
export function filterDeletedChatMessages<T extends { id: number; replyTo: { id: number } | null }>(
  messages: T[],
  deletedIds: ReadonlySet<number>,
): T[] {
  return messages.filter(message => !deletedIds.has(message.id))
    .map(message => message.replyTo && deletedIds.has(message.replyTo.id)
      ? { ...message, replyTo: null }
      : message)
}

/** 把畫面上的訊息與引用原文去重後分批查詢。 */
export async function syncChatDeletions(
  messages: ReadonlyArray<{ id: number; replyTo: { id: number } | null }>,
  fetchMissing: (ids: number[]) => Promise<number[]>,
  removeMessages: (ids: number[]) => void,
  isCurrent: () => boolean,
): Promise<void> {
  const ids = new Set<number>()
  for (const { id, replyTo } of messages) {
    ids.add(id)
    if (replyTo) ids.add(replyTo.id)
  }

  const snapshot = [...ids]
  for (let offset = 0; offset < snapshot.length && isCurrent(); offset += 50) {
    const missing = await fetchMissing(snapshot.slice(offset, offset + 50))
    if (!isCurrent()) return
    removeMessages(missing)
  }
}
