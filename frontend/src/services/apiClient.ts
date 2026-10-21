// 受保護 API 回傳 401 時通知最上層元件統一清除登入畫面。
export const authUnauthorizedEvent = 'auth:unauthorized'
let refreshPromise: Promise<string | null> | null = null
let lastRefresh: { from: string, to: string } | null = null

/** 多個 API 同時遇到 401 時，只送出一次輪替請求。 */
async function refreshAccessToken(oldToken: string): Promise<string | null> {
  refreshPromise ??= (async () => {
    try {
      const response = await fetch('/auth/refresh', { method: 'POST' })
      if (!response.ok) return null
      const body = await response.json() as { accessToken: string }
      // 使用者等待期間可能已登出或切換帳號，不能重新寫入舊工作階段。
      if (window.sessionStorage.getItem('accessToken') !== oldToken) return null
      window.sessionStorage.setItem('accessToken', body.accessToken)
      lastRefresh = { from: oldToken, to: body.accessToken }
      return body.accessToken
    } catch {
      return null
    }
  })()

  try { return await refreshPromise }
  finally { refreshPromise = null }
}

/** 登出前等待正在進行的輪替完成，讓伺服器撤銷最新 Cookie。 */
export async function waitForRefresh(): Promise<void> {
  await refreshPromise
}

/** 保存 API 錯誤的 HTTP 狀態碼與 Trace Id，畫面可以顯示並提供給後端查 Log。 */
export class ApiError extends Error {
  public readonly status: number
  public readonly traceId?: string

  constructor(
    message: string,
    status: number,
    traceId?: string,
  ) {
    // 呼叫 Error 的 Constructor 保存 message 與 Stack Trace。
    super(message)
    // 明確設定類別名稱，方便除錯工具辨識錯誤種類。
    this.name = 'ApiError'
    // erasableSyntaxOnly 不允許 Parameter Property，因此在 Constructor 內明確賦值。
    this.status = status
    this.traceId = traceId
  }
}

/** 將所有 API 的非成功回應統一轉成 Error。 */
export async function request(url: string, options: RequestInit = {}): Promise<Response> {
  let response = await fetch(url, options)
  let authorization = new Headers(options.headers).get('Authorization')

  if (response.status === 401 && authorization?.startsWith('Bearer ')) {
    const oldToken = authorization.slice(7)
    const currentToken = window.sessionStorage.getItem('accessToken')
    const nextToken = currentToken === oldToken
      ? await refreshAccessToken(oldToken)
      : lastRefresh?.from === oldToken && lastRefresh.to === currentToken ? currentToken : null
    if (nextToken && window.sessionStorage.getItem('accessToken') === nextToken) {
      const headers = new Headers(options.headers)
      authorization = `Bearer ${nextToken}`
      headers.set('Authorization', authorization)
      response = await fetch(url, { ...options, headers })
    }
  }

  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as {
      message?: string
      title?: string
      detail?: string
      traceId?: string
      errors?: Record<string, string[]>
    }

    const validationMessage = Object.values(body.errors ?? {}).flat()[0]

    // 舊請求的 401 不應清除使用者剛換成的新帳號。
    if (response.status === 401 && authorization === `Bearer ${window.sessionStorage.getItem('accessToken')}`) {
      window.dispatchEvent(new Event(authUnauthorizedEvent))
    }

    const fallbackMessage = response.status === 401
      ? '登入已失效，請重新登入。'
      : `請求失敗（HTTP ${response.status}）`

    // JWT Middleware 的 401 可能只有英文 title；Controller 的中文 detail 則優先顯示。
    const errorMessage = response.status === 401
      ? body.detail ?? body.message ?? validationMessage ?? fallbackMessage
      : body.message ?? validationMessage ?? body.detail ?? body.title ?? fallbackMessage

    throw new ApiError(errorMessage, response.status, body.traceId)
  }

  return response
}

/** 從目前分頁取得 JWT，建立受保護 API 使用的 Authorization Header。 */
export function getAuthorizationHeaders(): Record<string, string> {
  const accessToken = window.sessionStorage.getItem('accessToken')

  if (!accessToken) {
    throw new Error('請先登入再操作待辦事項。')
  }

  return { Authorization: `Bearer ${accessToken}` }
}
