import assert from 'node:assert/strict'
import { execFileSync } from 'node:child_process'
import { randomUUID } from 'node:crypto'

// 在已啟動的 Docker Compose 環境執行：node tests/auth-smoke.mjs。
const baseUrl = process.env.API_BASE_URL ?? 'http://localhost:8180'
const marker = `auth-test-${randomUUID().slice(0, 8)}`
const emails = [`${marker}-a@example.test`, `${marker}-b@example.test`]
const password = 'Testing-Password-123!'

async function send(path, { method = 'GET', body, cookie, bearer } = {}) {
  const response = await fetch(baseUrl + path, {
    method,
    headers: {
      ...(body && { 'Content-Type': 'application/json' }),
      ...(cookie && { Cookie: cookie }),
      ...(bearer && { Authorization: `Bearer ${bearer}` }),
    },
    body: body && JSON.stringify(body),
  })
  const text = await response.text()
  return {
    status: response.status,
    body: text ? JSON.parse(text) : null,
    cookie: response.headers.get('set-cookie')?.split(';')[0],
    contentType: response.headers.get('content-type'),
  }
}

try {
  for (const email of emails) {
    assert.equal((await send('/auth/register', { method: 'POST', body: { email, password } })).status, 201)
  }

  const loginA = await send('/auth/login', { method: 'POST', body: { email: emails[0], password } })
  const loginB = await send('/auth/login', { method: 'POST', body: { email: emails[1], password } })
  assert.equal(loginA.status, 200)
  assert.equal(loginB.status, 200)
  assert.ok(loginA.cookie)

  const refreshed = await send('/auth/refresh', { method: 'POST', cookie: loginA.cookie })
  assert.equal(refreshed.status, 200)
  assert.notEqual(refreshed.body.accessToken, loginA.body.accessToken)
  assert.ok(refreshed.cookie)
  assert.equal((await send('/auth/refresh', { method: 'POST', cookie: loginA.cookie })).status, 401)
  assert.equal((await send('/auth/refresh', { method: 'POST', cookie: refreshed.cookie })).status, 401)

  const todo = await send('/todos', {
    method: 'POST', body: { title: '授權整合測試' }, bearer: loginA.body.accessToken,
  })
  assert.equal(todo.status, 201)
  const forbidden = await send(`/todos/${todo.body.id}`, { method: 'DELETE', bearer: loginB.body.accessToken })
  assert.equal(forbidden.status, 404)
  assert.match(forbidden.contentType, /application\/problem\+json/)
  assert.ok(forbidden.body.traceId)

  const freshLogin = await send('/auth/login', { method: 'POST', body: { email: emails[0], password } })
  assert.equal((await send('/auth/logout', { method: 'POST', cookie: freshLogin.cookie })).status, 204)
  assert.equal((await send('/auth/refresh', { method: 'POST', cookie: freshLogin.cookie })).status, 401)
  console.log('PASS: JWT、Refresh 輪替與重放、跨會員隔離、Problem Details、登出撤銷')
} finally {
  // 測試使用隨機前綴；刪除會員時外鍵會一併清掉 Todo 與 Refresh Token。
  execFileSync('docker', ['compose', 'exec', '-T', 'db', 'psql', '-U', 'aspnetlearning',
    '-d', 'aspnetlearning', '-c', `DELETE FROM users WHERE "Email" LIKE '${marker}%';`],
  { stdio: 'ignore' })
}
