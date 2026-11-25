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
