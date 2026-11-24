import assert from 'node:assert/strict'
import test from 'node:test'
import { useChatReadingState } from '../src/composables/useChatReadingState.ts'

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
