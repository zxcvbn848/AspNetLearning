// interface 定義前端收到的 JSON 形狀，欄位必須與 ASP.NET Core 回應一致。
export interface TodoItem {
  id: number
  title: string
  isCompleted: boolean
  userId: number
}
