using AspNetLearning.Data;
using AspNetLearning.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AspNetLearning.Authentication;

/// <summary>集中註冊 JWT 驗證服務，讓 Program.cs 保留應用程式組裝流程。</summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>驗證 JWT 簽章、有效期限與所屬會員，並允許 SignalR Hub 取得瀏覽器 Token。</summary>
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT 簽章金鑰至少需要 32 bytes，避免使用過短金鑰降低 HMAC-SHA256 安全性。
        string jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("找不到 Jwt:Key 設定。");

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("Jwt:Key 至少需要 32 bytes。");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // 保留 JWT 原始 Claim 名稱，讓 Controller 可以直接讀取標準 sub Claim。
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // 瀏覽器的 WebSocket 無法設定 Authorization Header；只允許 Hub 從查詢字串讀取 JWT。
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/hubs/todos") &&
                            context.Request.Query.TryGetValue("access_token", out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    // JWT 簽章與有效期限通過後，再確認會員仍存在，避免已刪除帳號繼續使用舊 Token。
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null ||
                            !context.Principal.TryGetUserId(out int userId))
                        {
                            context.Fail("Token 缺少有效的會員 Id。");
                            return;
                        }

                        AppDbContext dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<AppDbContext>();

                        // shortcut: 每個受保護請求查詢一次 users，流量增加時再加入快取或 Security Stamp。
                        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == userId);

                        if (!userExists)
                        {
                            context.Fail("Token 所屬會員不存在。");
                        }
                    }
                };
            });
    }
}
