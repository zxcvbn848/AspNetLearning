using System.Diagnostics;

namespace AspNetLearning.Services;

/// <summary>限制同一會員在最近 10 秒內最多送出 5 則聊天室訊息。</summary>
public sealed class ChatSendLimiter
{
    private const int Limit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Queue<(int UserId, long Timestamp)> _accepted = new();
    private readonly Dictionary<int, int> _counts = new();

    public bool TryAcquire(int userId)
    {
        // 查詢與扣額必須在同一把鎖內，避免兩個分頁同時取得最後一個名額。
        lock (_gate)
        {
            long now = Stopwatch.GetTimestamp();
            while (_accepted.TryPeek(out var expired) &&
                   Stopwatch.GetElapsedTime(expired.Timestamp, now) >= Window)
            {
                _accepted.Dequeue();
                if (--_counts[expired.UserId] == 0)
                {
                    _counts.Remove(expired.UserId);
                }
            }

            int count = _counts.GetValueOrDefault(userId);
            if (count >= Limit) return false;

            _accepted.Enqueue((userId, now));
            _counts[userId] = count + 1;
            return true;
        }
    }
}
