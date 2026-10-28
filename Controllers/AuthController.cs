using AspNetLearning.Contracts;
using AspNetLearning.Extensions;
using AspNetLearning.Models;
using AspNetLearning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetLearning.Controllers;

/// <summary>
/// 接收會員註冊與後續登入相關的 HTTP 請求。
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(UserService userService, TokenService tokenService, RefreshTokenService refreshTokenService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _environment = environment;
    }

    /// <summary>
    /// 建立新會員。
    /// </summary>
    /// <returns>成功時回傳 HTTP 201；Email 已存在時回傳 HTTP 409。</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (request.Nickname is not null && request.Nickname.Trim().Length is < 2 or > 30)
            return BadRequest(new { message = "暱稱長度須介於 2 到 30 個字元。" });

        User? user = await _userService.RegisterAsync(request.Email, request.Password, request.Nickname);

        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "此 Email 已經註冊。");
        }

        return StatusCode(StatusCodes.Status201Created, UserResponse.FromUser(user));
    }

    /// <summary>
    /// 驗證會員帳號密碼並簽發 JWT。
    /// </summary>
    /// <returns>成功時回傳 HTTP 200 與 Token；失敗時回傳 HTTP 401。</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        User? user = await _userService.AuthenticateAsync(request.Email, request.Password);

        if (user is null)
        {
            // 不分辨帳號或密碼錯誤，避免外部得知某個 Email 是否存在。
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Email 或密碼不正確。");
        }

        string refreshToken = await _refreshTokenService.CreateAsync(user);
        SetRefreshCookie(refreshToken);
        return Ok(_tokenService.CreateToken(user));
    }

    /// <summary>使用 HttpOnly Cookie 換發 Access Token，並輪替 Refresh Token。</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Refresh()
    {
        string? cookie = Request.Cookies[RefreshTokenService.CookieName];
        if (string.IsNullOrEmpty(cookie))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "沒有有效的登入工作階段。");

        (User User, string Token)? result = await _refreshTokenService.RotateAsync(cookie);
        if (result is null)
        {
            DeleteRefreshCookie();
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "登入工作階段已失效，請重新登入。");
        }

        SetRefreshCookie(result.Value.Token);
        return Ok(_tokenService.CreateToken(result.Value.User));
    }

    /// <summary>撤銷目前工作階段並刪除瀏覽器 Cookie。</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        string? cookie = Request.Cookies[RefreshTokenService.CookieName];
        if (!string.IsNullOrEmpty(cookie)) await _refreshTokenService.RevokeAsync(cookie);
        DeleteRefreshCookie();
        return NoContent();
    }

    private void SetRefreshCookie(string token) => Response.Cookies.Append(
        RefreshTokenService.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_environment.IsDevelopment(),
            Path = "/auth",
            MaxAge = RefreshTokenService.Lifetime
        });

    private void DeleteRefreshCookie() => Response.Cookies.Delete(
        RefreshTokenService.CookieName, new CookieOptions { Path = "/auth" });

    /// <summary>
    /// 取得 JWT 所代表的目前登入會員。
    /// </summary>
    /// <returns>Token 有效時回傳 HTTP 200 與會員資料。</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        // 從已通過 JwtBearer 驗證的 ClaimsPrincipal 讀取標準 sub Claim。
        if (!User.TryGetUserId(out int userId))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Token 缺少有效的會員 Id。");
        }

        User? user = await _userService.GetByIdAsync(userId);

        if (user is null)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Token 對應的會員不存在。");
        }

        return Ok(UserResponse.FromUser(user));
    }

    /// <summary>修改目前登入會員的公開暱稱。</summary>
    [Authorize]
    [HttpPut("me/nickname")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<UserResponse>> UpdateNickname(UpdateNicknameRequest request)
    {
        if (!User.TryGetUserId(out int userId)) return Unauthorized();
        if (request.Nickname.Trim().Length is < 2 or > 30)
            return BadRequest(new { message = "暱稱長度須介於 2 到 30 個字元。" });

        User? user = await _userService.UpdateNicknameAsync(userId, request.Nickname);
        return user is null ? Unauthorized() : Ok(UserResponse.FromUser(user));
    }
}
