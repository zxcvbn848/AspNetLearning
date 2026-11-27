import assert from 'node:assert/strict'
import test from 'node:test'
import { useChatReadingState } from '../src/composables/useChatReadingState.ts'
import { filterDeletedChatMessages } from '../src/services/chatHistory.ts'

test('亂序廣播仍逐則計數，已載入的訊息不重複計數', () => {
  const state = useChatReadingState()
  const existing = new Map([[100, true]])
  state.updatePosition({ scrollHeight: 1000, scrollTop: 100, clientHeight: 400 })
  state.recordNewMessages([102], existing)
  existing.set(102, true)
  state.recordNewMessages([101, 102, 100], existing)
  assert.equal(state.isAtLatest.value, false)
  assert.equal(state.unreadCount.value, 2)
})

test('捲到底清除提示；停在底部接收訊息不累計未讀', () => {
  const state = useChatReadingState()
  state.updatePosition({ scrollHeight: 1000, scrollTop: 0, clientHeight: 400 })
  state.recordNewMessages([51], new Map())
  state.updatePosition({ scrollHeight: 1000, scrollTop: 600, clientHeight: 400 })
  assert.equal(state.isAtLatest.value, true)
  assert.equal(state.unreadCount.value, 0)
  state.recordNewMessages([52], new Map())
  assert.equal(state.unreadCount.value, 0)
})
test('刪除未讀原訊息後扣除提示，較晚到的歷史資料也只顯示已刪除的引用', () => {
  const state = useChatReadingState()
  state.updatePosition({ scrollHeight: 1000, scrollTop: 100, clientHeight: 400 })
  state.recordNewMessages([101, 102], new Map())
  state.forgetMessage(101)
  assert.equal(state.unreadCount.value, 1)

  const reply = { id: 102, content: '收到', replyTo: { id: 101, userId: 1, nickname: '小明', content: '今晚八點' } }
  const history = [{ id: 101, content: '今晚八點', replyTo: null }, reply]
  assert.deepEqual(filterDeletedChatMessages(history, new Set([101])), [
    { ...reply, replyTo: { id: 101, userId: null, nickname: null, content: null } },
  ])
})
