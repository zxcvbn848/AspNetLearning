namespace AspNetLearning.Contracts;

/// <summary>問候 API 回傳的 JSON 格式。</summary>
/// <param name="Message">包含網址名稱的問候文字。</param>
public sealed record HelloResponse(string Message);
