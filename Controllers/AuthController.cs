using AspNetLearning.Contracts;
using AspNetLearning.Models;
using AspNetLearning.Services;
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

    public AuthController(UserService userService, TokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 建立新會員。
    /// </summary>
    /// <returns>成功時回傳 HTTP 201；Email 已存在時回傳 HTTP 409。</returns>
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        User? user = await _userService.RegisterAsync(request.Email, request.Password);

        if (user is null)
        {
            return Conflict(new { message = "此 Email 已經註冊。" });
        }

        return StatusCode(StatusCodes.Status201Created, UserResponse.FromUser(user));
    }

    /// <summary>
    /// 驗證會員帳號密碼並簽發 JWT。
    /// </summary>
    /// <returns>成功時回傳 HTTP 200 與 Token；失敗時回傳 HTTP 401。</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        User? user = await _userService.AuthenticateAsync(request.Email, request.Password);

        if (user is null)
        {
            // 不分辨帳號或密碼錯誤，避免外部得知某個 Email 是否存在。
            return Unauthorized(new { message = "Email 或密碼不正確。" });
        }

        return Ok(_tokenService.CreateToken(user));
    }
}
