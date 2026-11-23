import assert from 'node:assert/strict'
import test from 'node:test'
import { useChatReadingState } from '../src/composables/useChatReadingState.ts'

test('閱讀舊訊息時累計新訊息，重複 Id 不重複計數', () => {
  const state = useChatReadingState()
  state.updatePosition({ scrollHeight: 1000, scrollTop: 100, clientHeight: 400 })
  state.recordNewMessages([51, 52])
  state.recordNewMessages([52, 53])
  assert.equal(state.isAtLatest.value, false)
  assert.equal(state.unreadCount.value, 3)
})

test('捲到底清除提示；停在底部接收訊息不累計未讀', () => {
  const state = useChatReadingState()
  state.updatePosition({ scrollHeight: 1000, scrollTop: 0, clientHeight: 400 })
  state.recordNewMessages([51])
  state.updatePosition({ scrollHeight: 1000, scrollTop: 600, clientHeight: 400 })
  assert.equal(state.isAtLatest.value, true)
  assert.equal(state.unreadCount.value, 0)
  state.recordNewMessages([52])
  assert.equal(state.unreadCount.value, 0)
})
