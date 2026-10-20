using System.Security.Cryptography;
using AspNetLearning.Data;
using AspNetLearning.Models;
using Microsoft.EntityFrameworkCore;

namespace AspNetLearning.Services;

/// <summary>簽發、輪替與撤銷保存於 HttpOnly Cookie 的 Refresh Token。</summary>
public sealed class RefreshTokenService
{
    public const string CookieName = "aspnetlearning_refresh";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
    private readonly AppDbContext _dbContext;

    public RefreshTokenService(AppDbContext dbContext) => _dbContext = dbContext;

    /// <summary>登入時建立新的工作階段；原始 Token 只交給瀏覽器。</summary>
    public async Task<string> CreateAsync(User user)
    {
        string token = NewToken();
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            FamilyId = Guid.NewGuid(),
            ExpiresAtUtc = DateTime.UtcNow.Add(Lifetime)
        });
        await _dbContext.SaveChangesAsync();
        return token;
    }

    /// <summary>每次更新都作廢舊 Token；重複使用舊 Token 時撤銷整個工作階段。</summary>
    public async Task<(User User, string Token)?> RotateAsync(string token)
    {
        if (token.Length > 128) return null;
        string hash = Hash(token);
        // 交易保證並發的舊 Token 重放可看見新 Token，並撤銷整個工作階段。
        return await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync<(User User, string Token)?>(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            RefreshToken? previous = await _dbContext.RefreshTokens
                .AsNoTracking().Include(item => item.User)
                .SingleOrDefaultAsync(item => item.TokenHash == hash);

            if (previous is null) return null;

            DateTime now = DateTime.UtcNow;
            if (previous.RevokedAtUtc is not null)
            {
                await RevokeFamilyAsync(previous.FamilyId);
                await transaction.CommitAsync();
                return null;
            }

            if (previous.ExpiresAtUtc <= now) return null;

            // 條件更新只有一個並發請求會成功，另一個會等到交易提交後再撤銷整個 Family。
            int updated = await _dbContext.RefreshTokens
                .Where(item => item.Id == previous.Id && item.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, now));
            if (updated == 0)
            {
                await RevokeFamilyAsync(previous.FamilyId);
                await transaction.CommitAsync();
                return null;
            }

            string next = NewToken();
            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = previous.UserId,
                TokenHash = Hash(next),
                FamilyId = previous.FamilyId,
                ExpiresAtUtc = now.Add(Lifetime)
            });
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return (previous.User, next);
        });
    }

    /// <summary>登出時撤銷目前工作階段，其他裝置的登入不受影響。</summary>
    public async Task RevokeAsync(string token)
    {
        if (token.Length > 128) return;
        string hash = Hash(token);
        await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            Guid? familyId = await _dbContext.RefreshTokens
                .Where(item => item.TokenHash == hash)
                .Select(item => (Guid?)item.FamilyId)
                .SingleOrDefaultAsync();
            if (familyId is null) return;

            // 先鎖定目前 Token；若更新同時發生，等待它提交後才能撤銷新 Token。
            await _dbContext.RefreshTokens.Where(item => item.TokenHash == hash)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, DateTime.UtcNow));
            await RevokeFamilyAsync(familyId.Value);
            await transaction.CommitAsync();
        });
    }

    private Task<int> RevokeFamilyAsync(Guid familyId) => _dbContext.RefreshTokens
        .Where(item => item.FamilyId == familyId && item.RevokedAtUtc == null)
        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, DateTime.UtcNow));

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
