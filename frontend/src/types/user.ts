/** 註冊 API 可以安全回傳的會員資料，不包含密碼雜湊。 */
export interface UserResponse {
  id: number
  email: string
  createdAtUtc: string
}

/** 登入 API 回傳的 JWT、到期時間與會員資料。 */
export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresAtUtc: string
  user: UserResponse
}
