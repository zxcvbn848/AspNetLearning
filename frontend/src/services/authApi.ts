import type { LoginResponse, UserResponse } from '../types/user'
import { request, waitForRefresh } from './apiClient'

/** 呼叫 POST /auth/register 建立會員。 */
export async function registerUser(email: string, password: string, nickname?: string): Promise<UserResponse> {
  const response = await request('/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, nickname }),
  })

  return response.json() as Promise<UserResponse>
}

/** 修改目前登入會員的公開暱稱。 */
export async function updateNickname(accessToken: string, nickname: string): Promise<UserResponse> {
  const response = await request('/auth/me/nickname', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` },
    body: JSON.stringify({ nickname }),
  })
  return response.json() as Promise<UserResponse>
}

/** 呼叫 POST /auth/login 驗證會員並取得 JWT。 */
export async function loginUser(email: string, password: string): Promise<LoginResponse> {
  const response = await request('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })

  return response.json() as Promise<LoginResponse>
}

/** 使用 Bearer Token 呼叫 GET /auth/me 取得目前會員。 */
export async function getCurrentUser(accessToken: string): Promise<UserResponse> {
  const response = await request('/auth/me', {
    headers: { Authorization: `Bearer ${accessToken}` },
  })

  return response.json() as Promise<UserResponse>
}

/** 撤銷目前的 Refresh Token，並請瀏覽器刪除 HttpOnly Cookie。 */
export async function logoutUser(): Promise<void> {
  await waitForRefresh()
  await request('/auth/logout', { method: 'POST' })
}
