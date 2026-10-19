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

    public AuthController(UserService userService)
    {
        _userService = userService;
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
}
