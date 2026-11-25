import assert from 'node:assert/strict'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { filterDeletedChatMessages, loadChatGap, syncChatDeletions } from '../src/services/chatHistory.ts'

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
  assert.equal(firstReceived[0].replyTo, null)

  const original = firstReceived[0]
  const quote = { id: original.id, userId: original.userId, nickname: '小明', content: text.trim() }
  await assert.rejects(second.invoke('SendReply', '無效回覆', 0), /找不到要回覆的訊息/)
  await assert.rejects(second.invoke('SendReply', '無效回覆', Number.MAX_SAFE_INTEGER), /找不到要回覆的訊息/)
  await second.invoke('SendReply', `暱稱測試 ${unique}`, original.id)
  await Promise.all([waitFor(firstReceived, 2), waitFor(secondReceived, 2)])
  assert.equal(firstReceived[1].nickname, '小華')
  assert.deepEqual(firstReceived[1].replyTo, quote)
  assert.deepEqual(secondReceived[1].replyTo, quote)

  const history = await second.invoke('GetRecentMessages')
  assert.ok(history.some(message => message.id === firstReceived[0].id))
  assert.equal(history.find(message => message.id === firstReceived[0].id).nickname, '小明')
  assert.deepEqual(history.find(message => message.id === firstReceived[1].id)?.replyTo, quote)
  await second.stop()
  // 另一個分頁離線時送超過 50 則，驗證整段跨頁補收。
  const afterId = history.at(-1).id
  // 多位會員各送最多五則，保持正式限流規則，也能快速建立 51 則測試資料。
  for (let start = 0; start < 51; start += 5) {
    const sender = connect(await register(`chat-seed-${start}-${unique}@example.com`), [])
    await sender.start()
    try {
      for (let index = start; index < Math.min(start + 5, 51); index++) {
        const content = `分頁測試 ${unique} ${index}`
        if (index === 0) await sender.invoke('SendReply', content, original.id)
        else await sender.invoke('SendMessage', content)
      }
    } finally {
      await sender.stop()
    }
  }
  await waitFor(firstReceived, 53)
  const expected = firstReceived.slice(2)
  const reconnected = connect(secondToken, [])
  await reconnected.start()
  try {
    const recent = await reconnected.invoke('GetRecentMessages')
    const throughId = recent.at(-1).id
    await first.invoke('SendMessage', `補收期間的新訊息 ${unique}`)
    const recovered = []
    const pageSizes = []
    await loadChatGap(afterId, throughId,
      (cursor, end) => reconnected.invoke('GetNewerMessages', cursor, end),
      async page => { pageSizes.push(page.length); recovered.push(...page) },
      () => true)
    assert.deepEqual(pageSizes, [50, 1])
    assert.deepEqual(recovered.map(message => message.id), expected.map(message => message.id))
    assert.deepEqual(recovered[0].replyTo, quote)
    await assert.rejects(reconnected.invoke('GetNewerMessages', throughId, throughId))
  } finally {
    await reconnected.stop()
  }
  const latest = await first.invoke('GetRecentMessages')
  assert.equal(latest.length, 50)
  const older = await first.invoke('GetOlderMessages', latest[0].id)
  assert.ok(older.length > 0 && older.length <= 50)
  assert.ok(older.every(message => message.id < latest[0].id))
  assert.deepEqual(older.find(message => message.content === `分頁測試 ${unique} 0`)?.replyTo, quote)
  assert.ok(older.every((message, index) => index === 0 || older[index - 1].id < message.id))
  assert.deepEqual(await first.invoke('GetOlderMessages', 1), [])
  await assert.rejects(first.invoke('GetOlderMessages', 0))
  // 新訊息不會改變既定游標之前的資料。
  await first.invoke('SendMessage', `分頁期間的新訊息 ${unique}`)
  assert.deepEqual(await first.invoke('GetOlderMessages', latest[0].id), older)

  const limitedToken = await register(`chat-limit-${unique}@example.com`)
  const tabA = connect(limitedToken, [])
  const tabB = connect(limitedToken, [])
  try {
    await Promise.all([tabA.start(), tabB.start()])
    const prefix = `限流測試 ${unique} `
    const initialBroadcastCount = firstReceived.length
    await tabA.invoke('SendMessage', prefix + '0')
    await tabB.invoke('SendReply', prefix + '1', original.id)
    const results = await Promise.allSettled([
      tabA.invoke('SendMessage', prefix + '2'),
      tabB.invoke('SendReply', prefix + '3', original.id),
      tabA.invoke('SendMessage', prefix + '4'),
      tabB.invoke('SendReply', prefix + '5', original.id),
    ])
    const denied = results.filter(result => result.status === 'rejected')
    assert.equal(denied.length, 1)
    assert.match(denied[0].reason.message, /CHAT_RATE_LIMIT$/)
    await assert.rejects(tabB.invoke('SendMessage', prefix + 'blocked'), /CHAT_RATE_LIMIT$/)

    const otherUserMessage = `另一會員 ${unique}`
    await first.invoke('SendMessage', otherUserMessage)
    await waitFor(firstReceived, initialBroadcastCount + 6)
    const accepted = [prefix + '0', prefix + '1',
      ...results.flatMap((result, index) => result.status === 'fulfilled' ? [prefix + (index + 2)] : [])].sort()
    const broadcastContents = firstReceived.map(message => message.content)
    assert.ok(broadcastContents.includes(otherUserMessage))
    assert.deepEqual(broadcastContents.filter(content => content.startsWith(prefix)).sort(), accepted)

    const persisted = await tabA.invoke('GetRecentMessages')
    assert.deepEqual(persisted.map(message => message.content).filter(content => content.startsWith(prefix)).sort(), accepted)

    await new Promise(resolve => setTimeout(resolve, 10_000))
    await tabB.invoke('SendMessage', prefix + 'retry')
    await waitFor(firstReceived, initialBroadcastCount + 7)
    assert.equal(firstReceived.at(-1).content, prefix + 'retry')
  } finally {
    await Promise.all([tabA.stop(), tabB.stop()])
  }

  const firstDeleted = []
  const secondDeleted = []
  first.on('MessageDeleted', id => firstDeleted.push(id))
  second.on('MessageDeleted', id => secondDeleted.push(id))
  const offline = connect(secondToken, [])
  const offlineDeleted = []
  offline.on('MessageDeleted', id => offlineDeleted.push(id))
  await offline.start()
  let snapshot
  try {
    const recent = await offline.invoke('GetRecentMessages')
    const earlier = await offline.invoke('GetOlderMessages', recent[0].id)
    // 模擬原文不在畫面，但已經載入引用它的回覆。
    snapshot = [...earlier, ...recent].filter(message => message.id !== original.id)
  } finally {
    await offline.stop()
  }
  const reply = snapshot.find(message => message.content === `分頁測試 ${unique} 0`)
  const visible = snapshot.find(message => message.content === `分頁期間的新訊息 ${unique}`)
  assert.deepEqual(reply?.replyTo, quote)
  assert.ok(visible)

  await second.start()
  await assert.rejects(second.invoke('DeleteMessage', original.id), /找不到可刪除的訊息/)
  await first.invoke('DeleteMessage', original.id)
  await Promise.all([waitFor(firstDeleted, 1), waitFor(secondDeleted, 1)])
  await assert.rejects(first.invoke('DeleteMessage', original.id), /找不到可刪除的訊息/)

  const afterDeletion = await second.invoke('GetNewerMessages', original.id - 1, firstReceived[1].id)
  assert.deepEqual(afterDeletion.map(message => message.id), [firstReceived[1].id])
  assert.equal(afterDeletion[0].replyTo, null)
  assert.deepEqual(firstDeleted, [original.id])
  assert.deepEqual(secondDeleted, [original.id])
  await first.invoke('DeleteMessage', visible.id)
  assert.deepEqual(offlineDeleted, [])
  await offline.start()
  try {
    for (const invalid of [[], [0], Array(51).fill(original.id)]) {
      await assert.rejects(offline.invoke('GetMissingMessageIds', invalid), /請提供 1 到 50 個有效的訊息 Id/)
    }
    const batchSizes = []
    const queriedIds = []
    const missingIds = new Set()
    await syncChatDeletions(snapshot, async ids => {
      batchSizes.push(ids.length)
      queriedIds.push(...ids)
      return offline.invoke('GetMissingMessageIds', ids)
    }, ids => {
      for (const id of ids) missingIds.add(id)
      snapshot = filterDeletedChatMessages(snapshot, missingIds)
    }, () => true)
    assert.ok(batchSizes.length > 1 && batchSizes.every(size => size <= 50))
    assert.equal(queriedIds.length, new Set(queriedIds).size)
    assert.deepEqual(missingIds, new Set([original.id, visible.id]))
    assert.equal(snapshot.find(message => message.id === reply.id)?.replyTo, null)
    assert.ok(!snapshot.some(message => message.id === visible.id))
  } finally {
    await offline.stop()
  }
  console.log('PASS: 認證、回覆、分頁、重連補收、發送限流、刪除權限與重連同步刪除')
} finally {
  await Promise.all([first.stop(), second.stop()])
}
