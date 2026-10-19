using AspNetLearning.Data;
using AspNetLearning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AspNetLearning.Services;

/// <summary>
/// 處理會員註冊與密碼雜湊。
/// </summary>
public sealed class UserService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 建立會員；Email 已存在時回傳 null。
    /// </summary>
    public async Task<User?> RegisterAsync(string email, string password)
    {
        // 正規化讓 Andy@example.com 與 andy@example.com 被視為同一個帳號。
        string normalizedEmail = email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(user => user.Email == normalizedEmail))
        {
            return null;
        }

        User user = new()
        {
            Email = normalizedEmail,
            // PasswordHash 先給暫時值，下一行再使用完整 User 產生雜湊。
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
            return user;
        }
        // 唯一索引是最後一道保護，避免兩個同時註冊請求建立重複 Email。
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return null;
        }
    }

    /// <summary>
    /// 驗證 Email 與密碼；任一項不正確時回傳 null。
    /// </summary>
    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        User? user = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail);

        if (user is null)
        {
            return null;
        }

        PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // ASP.NET Core 日後提升雜湊參數時，成功登入即可更新舊雜湊。
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }
}
