using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AspNetLearning.Extensions;

/// <summary>
/// 提供從已驗證 JWT Claim 讀取會員資料的方法。
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>嘗試將 JWT 的 sub Claim 解析為會員 Id。</summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
    {
        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(subject, out userId);
    }
}
