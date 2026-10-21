using AspNetLearning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace AspNetLearning.OpenApi;

/// <summary>設定本機學習用的 OpenAPI 規格與互動文件。</summary>
public static class ApiDocumentation
{
    /// <summary>將 JWT、Refresh Token Cookie 與各端點的授權需求加入文件。</summary>
    public static void AddLearningOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "AspNetLearning API",
                    Version = "v1",
                    Description = "會員、待辦事項與 Refresh Token API。先註冊／登入，再使用 JWT 呼叫受保護端點。"
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    },
                    ["RefreshCookie"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Cookie,
                        Name = RefreshTokenService.CookieName,
                        Description = "登入後由瀏覽器保存的 HttpOnly Cookie；無法從 JavaScript 讀取。"
                    }
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                // 只標記真正受 [Authorize] 保護的端點，登入與註冊保持公開。
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                    });
                }

                if (context.Description.RelativePath == "auth/refresh")
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("RefreshCookie", context.Document)] = []
                    });
                }

                return Task.CompletedTask;
            });
        });
    }
}
