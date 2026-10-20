import assert from 'node:assert/strict'
import test from 'node:test'
import { request } from '../src/services/apiClient.ts'

test('舊 Token 的 401 不會登出新帳號；目前 Token 的 401 會通知畫面', async () => {
  const events = []
  const storage = new Map([['accessToken', 'new-token']])
  globalThis.window = {
    sessionStorage: { getItem: (key) => storage.get(key) ?? null },
    dispatchEvent: (event) => events.push(event.type),
  }
  globalThis.fetch = async () => new Response('{}', { status: 401 })

  await assert.rejects(request('/todos', { headers: { Authorization: 'Bearer old-token' } }))
  assert.deepEqual(events, [])

  await assert.rejects(request('/todos', { headers: { Authorization: 'Bearer new-token' } }))
  assert.deepEqual(events, ['auth:unauthorized'])
})
