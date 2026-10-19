import type { TodoItem } from '../types/todo'
import { request } from './apiClient'

// 開發時由 Vite Proxy 轉送，日後同網域部署也不需修改網址。
const apiUrl = '/todos'

/** 取得所有待辦事項。 */
export async function getTodoItems(): Promise<TodoItem[]> {
  const response = await request(apiUrl)
  return response.json() as Promise<TodoItem[]>
}

/** 建立一筆待辦事項。 */
export async function createTodoItem(title: string): Promise<TodoItem> {
  const response = await request(apiUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title }),
  })

  return response.json() as Promise<TodoItem>
}

/** 更新指定待辦事項。 */
export async function updateTodoItem(
  id: number,
  title: string,
  isCompleted: boolean,
): Promise<TodoItem> {
  const response = await request(`${apiUrl}/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, isCompleted }),
  })

  return response.json() as Promise<TodoItem>
}

/** 刪除指定待辦事項；HTTP 204 沒有 JSON 可解析。 */
export async function deleteTodoItem(id: number): Promise<void> {
  await request(`${apiUrl}/${id}`, { method: 'DELETE' })
}
