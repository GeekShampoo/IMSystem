using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Responses.User;
using MediatR;

namespace IMSystem.Server.Core.Features.User.Commands;

public class RegisterUserCommand : IRequest<Result<UserDto>> // 通常注册成功后会返回用户信息 DTO
{
    public string Username { get; set; } = null!; // 用户名
    public string Email { get; set; } = null!;    // 邮箱
    public string Password { get; set; } = null!;   // 密码
    // public string? FirstName { get; set; } // 可选：姓
    // public string? LastName { get; set; }  // 可选：名
}