using IMSystem.Protocol.Common;
using IMSystem.Protocol.Enums;
using IMSystem.Server.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMSystem.Server.Web.Controllers;

/// <summary>
/// 基础API控制器，提供统一的错误处理方法等通用功能
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// 获取当前已认证用户的ID。
    /// 如果用户未认证或令牌无效，抛出UnauthorizedAccessException。
    /// </summary>
    protected Guid CurrentUserId
    {
        get
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("无法从令牌中识别有效的用户ID。");
            }
            return userId.Value;
        }
    }

    /// <summary>
    /// 处理包含值的 MediatR 结果。
    /// </summary>
    /// <typeparam name="TValue">结果值的类型。</typeparam>
    /// <param name="result">MediatR 返回的 Result 对象。</param>
    /// <param name="successAction">成功时执行的操作，接收结果值并返回 IActionResult。</param>
    /// <param name="conflictErrorCode">（可选）指定一个错误码，如果匹配，则返回 Conflict (409) 响应。</param>
    /// <returns>相应的 IActionResult。</returns>
    protected IActionResult HandleResult<TValue>(
        Result<TValue> result,
        Func<TValue, IActionResult> successAction,
        string? conflictErrorCode = null)
    {
        if (result.IsSuccess)
        {
            return successAction(result.Value);
        }

        if (!string.IsNullOrEmpty(conflictErrorCode) && result.Error?.Code == conflictErrorCode)
        {
            return Conflict(new ApiErrorResponse(
                StatusCodes.Status409Conflict,
                result.Error.Message,
                detail: null,
                errorCode: result.Error.Code));
        }
        return HandleFailure(result);
    }

    /// <summary>
    /// 处理不包含值的 MediatR 结果 (例如用于 Command)。
    /// </summary>
    /// <param name="result">MediatR 返回的 Result 对象。</param>
    /// <param name="successAction">成功时执行的操作，返回 IActionResult (通常是 NoContent 或 Ok)。</param>
    /// <param name="conflictErrorCode">（可选）指定一个错误码，如果匹配，则返回 Conflict (409) 响应。</param>
    /// <returns>相应的 IActionResult。</returns>
    protected IActionResult HandleResult(
        Result result,
        Func<IActionResult> successAction,
        string? conflictErrorCode = null)
    {
        if (result.IsSuccess)
        {
            return successAction();
        }
        if (!string.IsNullOrEmpty(conflictErrorCode) && result.Error?.Code == conflictErrorCode)
        {
            return Conflict(new ApiErrorResponse(
                StatusCodes.Status409Conflict,
                result.Error.Message,
                detail: null,
                errorCode: result.Error.Code));
        }
        return HandleFailure(result);
    }

    /// <summary>
    /// 处理失败的Result对象，将其转换为ApiErrorResponse
    /// </summary>
    /// <typeparam name="T">Result的泛型类型</typeparam>
    /// <param name="result">要处理的Result对象</param>
    /// <returns>包含ApiErrorResponse的ActionResult</returns>
    protected IActionResult HandleFailure<T>(Result<T> result)
    {
        var statusCode = result.Error.Code switch
        {
            // 根据错误代码映射到适当的HTTP状态码
            "Validation.Error" => StatusCodes.Status400BadRequest,
            "Entity.NotFound" => StatusCodes.Status404NotFound,
            "Authorization.Failed" => StatusCodes.Status401Unauthorized,
            "Access.Denied" => StatusCodes.Status403Forbidden,
            "Operation.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest // 默认使用400
        };

        var response = new ApiErrorResponse(
            statusCode,
            result.Error.Message,
            detail: null,
            errorCode: result.Error.Code
        );

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// 处理失败的非泛型Result对象，将其转换为ApiErrorResponse
    /// </summary>
    /// <param name="result">要处理的Result对象</param>
    /// <returns>包含ApiErrorResponse的ActionResult</returns>
    protected IActionResult HandleFailure(Result result)
    {
        var statusCode = result.Error.Code switch
        {
            // 根据错误代码映射到适当的HTTP状态码
            "Validation.Error" => StatusCodes.Status400BadRequest,
            "Entity.NotFound" => StatusCodes.Status404NotFound,
            "Authorization.Failed" => StatusCodes.Status401Unauthorized,
            "Access.Denied" => StatusCodes.Status403Forbidden,
            "Operation.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest // 默认使用400
        };

        var response = new ApiErrorResponse(
            statusCode,
            result.Error.Message,
            detail: null,
            errorCode: result.Error.Code
        );

        return StatusCode(statusCode, response);
    }
}