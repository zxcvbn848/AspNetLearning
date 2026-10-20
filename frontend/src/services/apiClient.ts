// 受保護 API 回傳 401 時通知最上層元件統一清除登入畫面。
export const authUnauthorizedEvent = 'auth:unauthorized'

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
  const response = await fetch(url, options)

  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as {
      message?: string
      title?: string
      detail?: string
      traceId?: string
      errors?: Record<string, string[]>
    }

    const validationMessage = Object.values(body.errors ?? {}).flat()[0]
    const authorization = new Headers(options.headers).get('Authorization')

    // 舊請求的 401 不應清除使用者剛換成的新帳號。
    if (response.status === 401 && authorization === `Bearer ${window.sessionStorage.getItem('accessToken')}`) {
      window.dispatchEvent(new Event(authUnauthorizedEvent))
    }

    const fallbackMessage = response.status === 401
      ? '登入已失效，請重新登入。'
      : `請求失敗（HTTP ${response.status}）`

    // JWT Middleware 產生的 Problem Details title 是 Unauthorized；401 優先使用既有中文提示。
    const errorMessage = response.status === 401
      ? body.message ?? validationMessage ?? fallbackMessage
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
