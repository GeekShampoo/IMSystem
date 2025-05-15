using IMSystem.Protocol.DTOs.Requests.Auth;
using IMSystem.Server.Core.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IMSystem.Server.Core.Features.User.Commands;
using Microsoft.AspNetCore.Http;
using IMSystem.Protocol.Common;
using IMSystem.Server.Web.Common;

namespace IMSystem.Server.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AuthenticationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求参数</param>
        /// <returns>登录成功则返回 Token 和用户信息，否则返回错误。</returns>
        [HttpPost("login")]
        [AllowAnonymous] // 登录接口允许匿名访问
        [ProducesResponseType(typeof(IMSystem.Protocol.DTOs.Responses.Auth.LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)] // 保留，认证端点的核心失败场景
        // 400 BadRequest 由模型验证和 DefaultApiConventions.Post 约定覆盖
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "请求参数验证失败", errorCode: "Validation.Failed"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var command = new LoginCommand(request.Username, request.Password, ipAddress);
            var result = await _mediator.Send(command);

            if (result.IsFailure || result.Value == null)
            {
                // 如果 result.Error.Message 可用，则使用它，否则使用通用消息
                string errorMessage = result.Error?.Message ?? "用户名或密码错误。";
                string errorCode = result.Error?.Code ?? "Auth.InvalidCredentials";
                int statusCode = StatusCodes.Status401Unauthorized;

                // 对已停用帐户的特定处理
                if (errorCode == "Auth.AccountDeactivated")
                {
                    return Unauthorized(new ApiErrorResponse(statusCode, errorMessage, errorCode: "AccountDeactivated"));
                }
                
                // 对于其他身份验证失败（用户未找到、密码无效）
                return Unauthorized(new ApiErrorResponse(statusCode, errorMessage, errorCode: "AuthenticationFailed"));
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// 使用令牌验证用户的电子邮件地址。
        /// </summary>
        /// <param name="token">从查询字符串中获取的电子邮件验证令牌。</param>
        /// <returns>成功或错误消息。</returns>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        // 400 BadRequest 由 DefaultApiConventions.GetList 或 DoAction 约定覆盖
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, "验证令牌不能为空", errorCode: "Validation.EmptyToken"));
            }

            var command = new VerifyEmailCommand(token);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                string errorMessage = result.Error?.Message ?? "电子邮件验证失败";
                string errorCode = result.Error?.Code ?? "Auth.VerificationFailed";
                string apiErrorCode;
                
                // 根据业务错误代码映射到标准错误码
                switch (errorCode)
                {
                    case "Auth.TokenExpired":
                        apiErrorCode = "TokenExpired";
                        break;
                    case "Auth.TokenInvalid":
                        apiErrorCode = "TokenInvalid";
                        break;
                    default:
                        apiErrorCode = "ValidationFailed";
                        break;
                }
                
                return BadRequest(new ApiErrorResponse(StatusCodes.Status400BadRequest, errorMessage, errorCode: apiErrorCode));
            }

            // 考虑返回成功页面或简单的成功消息
            return Ok(new { Message = "邮箱验证成功" });
        }
    }
}