/** 註冊 API 可以安全回傳的會員資料，不包含密碼雜湊。 */
export interface UserResponse {
  id: number
  email: string
  createdAtUtc: string
}
