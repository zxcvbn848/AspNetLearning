// 受保護 API 回傳 401 時通知最上層元件統一清除登入畫面。
export const authUnauthorizedEvent = 'auth:unauthorized'

/** 將所有 API 的非成功回應統一轉成 Error。 */
export async function request(url: string, options: RequestInit = {}): Promise<Response> {
  const response = await fetch(url, options)

  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as {
      message?: string
      errors?: Record<string, string[]>
    }

    const validationMessage = Object.values(body.errors ?? {}).flat()[0]
    const isProtectedRequest = new Headers(options.headers).has('Authorization')

    if (response.status === 401 && isProtectedRequest) {
      window.dispatchEvent(new Event(authUnauthorizedEvent))
    }

    const fallbackMessage = response.status === 401
      ? '登入已失效，請重新登入。'
      : `請求失敗（HTTP ${response.status}）`

    throw new Error(body.message ?? validationMessage ?? fallbackMessage)
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
