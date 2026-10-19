/** 將所有 API 的非成功回應統一轉成 Error。 */
export async function request(url: string, options: RequestInit = {}): Promise<Response> {
  const response = await fetch(url, options)

  if (!response.ok) {
    const body = await response.json().catch(() => ({})) as {
      message?: string
      errors?: Record<string, string[]>
    }

    const validationMessage = Object.values(body.errors ?? {}).flat()[0]
    throw new Error(body.message ?? validationMessage ?? `請求失敗（HTTP ${response.status}）`)
  }

  return response
}
