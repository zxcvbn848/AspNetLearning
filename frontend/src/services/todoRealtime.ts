import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr'
import { getRealtimeAccessToken } from './apiClient'

/** 建立會員專屬的 SignalR 連線；服務只傳變更通知，資料仍由 Todo API 讀取。 */
export function connectTodoUpdates(onChanged: () => void, onState: (state: 'connected' | 'reconnecting' | 'disconnected') => void): HubConnection {
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/todos', { accessTokenFactory: getRealtimeAccessToken })
    .withAutomaticReconnect()
    .build()

  connection.on('TodosChanged', onChanged)
  connection.onreconnecting(() => onState('reconnecting'))
  connection.onreconnected(() => {
    onState('connected')
    onChanged() // 斷線期間可能漏掉通知，重連後以 API 的最新資料為準。
  })
  connection.onclose(() => onState('disconnected'))
  return connection
}
