import assert from 'node:assert/strict'
import { HubConnectionBuilder } from '@microsoft/signalr'

const baseUrl = process.env.API_BASE_URL ?? 'http://app:8080'
const unique = `${Date.now()}-${Math.random().toString(36).slice(2)}`

async function register(email) {
  const password = 'Learning123!'
  const registered = await fetch(`${baseUrl}/auth/register`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  assert.equal(registered.status, 201)
  const login = await fetch(`${baseUrl}/auth/login`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  assert.equal(login.status, 200)
  return (await login.json()).accessToken
}

function connection(token, onChanged) {
  const hub = new HubConnectionBuilder().withUrl(`${baseUrl}/hubs/todos`, {
    accessTokenFactory: () => token,
  }).build()
  hub.on('TodosChanged', onChanged)
  return hub
}

async function waitForEvents(expected) {
  const deadline = Date.now() + 2000
  while (ownEvents < expected || secondEvents < expected) {
    assert.ok(Date.now() < deadline, `等待第 ${expected} 次通知逾時`)
    await new Promise(resolve => setTimeout(resolve, 20))
  }
}

const ownerToken = await register(`realtime-owner-${unique}@example.com`)
const otherToken = await register(`realtime-other-${unique}@example.com`)
const unauthorized = await fetch(`${baseUrl}/hubs/todos/negotiate?negotiateVersion=1`, { method: 'POST' })
assert.equal(unauthorized.status, 401)
let ownEvents = 0
let secondEvents = 0
let otherEvents = 0
const connections = [
  connection(ownerToken, () => ownEvents++),
  connection(ownerToken, () => secondEvents++),
  connection(otherToken, () => otherEvents++),
]

try {
  await Promise.all(connections.map(hub => hub.start()))
  const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${ownerToken}` }
  const created = await fetch(`${baseUrl}/todos`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ title: 'SignalR 測試' }),
  })
  assert.equal(created.status, 201)
  const todo = await created.json()
  await waitForEvents(1)
  assert.equal(ownEvents, 1)
  assert.equal(secondEvents, 1)
  assert.equal(otherEvents, 0)

  const updated = await fetch(`${baseUrl}/todos/${todo.id}`, {
    method: 'PUT', headers, body: JSON.stringify({ title: 'SignalR 已更新', isCompleted: true }),
  })
  assert.equal(updated.status, 200)
  const deleted = await fetch(`${baseUrl}/todos/${todo.id}`, {
    method: 'DELETE', headers: { Authorization: `Bearer ${ownerToken}` },
  })
  assert.equal(deleted.status, 204)
  await waitForEvents(3)
  assert.equal(ownEvents, 3)
  assert.equal(secondEvents, 3)
  assert.equal(otherEvents, 0)
  console.log('PASS: 未登入拒絕連線；新增、更新、刪除只通知同一會員')
} finally {
  await Promise.all(connections.map(hub => hub.stop()))
}
