import assert from 'node:assert/strict'
import { HubConnectionBuilder } from '@microsoft/signalr'

const baseUrl = process.env.API_BASE_URL ?? 'http://app:8080'
const unique = `${Date.now()}-${Math.random().toString(36).slice(2)}`

async function register(email, nickname) {
  const password = 'Learning123!'
  const registered = await fetch(`${baseUrl}/auth/register`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, nickname }),
  })
  assert.equal(registered.status, 201)
  const login = await fetch(`${baseUrl}/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  assert.equal(login.status, 200)
  return (await login.json()).accessToken
}

function connect(token, received) {
  const hub = new HubConnectionBuilder().withUrl(`${baseUrl}/hubs/chat`, {
    accessTokenFactory: () => token,
  }).build()
  hub.on('MessageReceived', message => received.push(message))
  return hub
}

async function waitFor(received, count) {
  const deadline = Date.now() + 2000
  while (received.length < count) {
    assert.ok(Date.now() < deadline, '等待聊天室廣播逾時')
    await new Promise(resolve => setTimeout(resolve, 20))
  }
}

const unauthorized = await fetch(`${baseUrl}/hubs/chat/negotiate?negotiateVersion=1`, { method: 'POST' })
assert.equal(unauthorized.status, 401)
const firstToken = await register(`chat-first-${unique}@example.com`, '小明')
const secondToken = await register(`chat-second-${unique}@example.com`)
const renamed = await fetch(`${baseUrl}/auth/me/nickname`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${secondToken}` },
  body: JSON.stringify({ nickname: '  小華  ' }),
})
assert.equal(renamed.status, 200)
assert.equal((await renamed.json()).nickname, '小華')
const firstReceived = []
const secondReceived = []
const first = connect(firstToken, firstReceived)
const second = connect(secondToken, secondReceived)

try {
  await Promise.all([first.start(), second.start()])
  const before = await first.invoke('GetRecentMessages')
  await assert.rejects(first.invoke('SendMessage', '   '))
  await assert.rejects(first.invoke('SendMessage', 'x'.repeat(501)))
  assert.equal((await first.invoke('GetRecentMessages')).length, before.length)

  const text = `  你好 ${unique}  `
  await first.invoke('SendMessage', text)
  await Promise.all([waitFor(firstReceived, 1), waitFor(secondReceived, 1)])
  assert.equal(firstReceived[0].content, text.trim())
  assert.equal(firstReceived[0].nickname, '小明')
  assert.equal(secondReceived[0].id, firstReceived[0].id)
  assert.ok(!('email' in firstReceived[0]))

  await second.invoke('SendMessage', `暱稱測試 ${unique}`)
  await Promise.all([waitFor(firstReceived, 2), waitFor(secondReceived, 2)])
  assert.equal(firstReceived[1].nickname, '小華')

  const history = await second.invoke('GetRecentMessages')
  assert.ok(history.some(message => message.id === firstReceived[0].id))
  assert.equal(history.find(message => message.id === firstReceived[0].id).nickname, '小明')
  await second.stop()
  const reconnected = connect(secondToken, [])
  await reconnected.start()
  try {
    assert.ok((await reconnected.invoke('GetRecentMessages')).some(message => message.id === firstReceived[0].id))
  } finally {
    await reconnected.stop()
  }
  console.log('PASS: 未登入拒絕、訊息驗證、跨會員廣播與資料庫歷史紀錄')
} finally {
  await Promise.all([first.stop(), second.stop()])
}
