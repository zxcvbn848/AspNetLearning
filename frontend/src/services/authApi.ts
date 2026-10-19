import type { UserResponse } from '../types/user'
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
