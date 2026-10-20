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

test('兩個同時失效的 API 共用一次 Refresh，並用新 Token 重試', async () => {
  const storage = new Map([['accessToken', 'expired-token']])
  globalThis.window = {
    sessionStorage: {
      getItem: (key) => storage.get(key) ?? null,
      setItem: (key, value) => storage.set(key, value),
    },
    dispatchEvent: () => assert.fail('成功續期不應觸發登出'),
  }
  let refreshCount = 0
  globalThis.fetch = async (url, options) => {
    if (url === '/auth/refresh') {
      refreshCount++
      return Response.json({ accessToken: 'renewed-token' })
    }
    const token = new Headers(options.headers).get('Authorization')
    return token === 'Bearer renewed-token'
      ? Response.json({ ok: true })
      : new Response('{}', { status: 401 })
  }

  const requests = await Promise.all([
    request('/todos', { headers: { Authorization: 'Bearer expired-token' } }),
    request('/auth/me', { headers: { Authorization: 'Bearer expired-token' } }),
  ])
  assert.deepEqual(requests.map((response) => response.status), [200, 200])
  assert.equal(refreshCount, 1)
  assert.equal(storage.get('accessToken'), 'renewed-token')
})
