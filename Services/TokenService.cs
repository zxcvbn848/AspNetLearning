using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspNetLearning.Contracts;
using AspNetLearning.Models;
using Microsoft.IdentityModel.Tokens;

namespace AspNetLearning.Services;

/// <summary>
/// 根據會員資料簽發 JWT Access Token。
/// </summary>
public sealed class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>建立包含會員 Id 與 Email 的 JWT。</summary>
    public LoginResponse CreateToken(User user)
    {
        string issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("找不到 Jwt:Issuer 設定。");
        string audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("找不到 Jwt:Audience 設定。");
        string key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("找不到 Jwt:Key 設定。");
        int expiresMinutes = _configuration.GetValue<int>("Jwt:ExpiresMinutes");

        if (expiresMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpiresMinutes 必須大於 0。");
        }

        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        Claim[] claims =
        [
            // sub 是 JWT 標準的 Subject，這裡用來保存會員 Id。
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            // jti 是每一枚 Token 的唯一識別碼。
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new LoginResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc,
            User = UserResponse.FromUser(user)
        };
    }
}
