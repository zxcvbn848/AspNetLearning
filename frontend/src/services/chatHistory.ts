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
