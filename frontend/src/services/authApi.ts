import type { LoginResponse, UserResponse } from '../types/user'
import { request } from './apiClient'

/** 呼叫 POST /auth/register 建立會員。 */
export async function registerUser(email: string, password: string): Promise<UserResponse> {
  const response = await request('/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
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
