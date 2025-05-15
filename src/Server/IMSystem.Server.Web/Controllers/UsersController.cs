using AutoMapper;
using IMSystem.Protocol.DTOs.Requests.User;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Web.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IMSystem.Server.Core.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR; // Added for IMediator
using IMSystem.Server.Core.Features.User.Queries; // Added for GetUserByIdQuery
using IMSystem.Protocol.Enums; // Added for ApiErrorCode
using Microsoft.AspNetCore.Http; // Added for StatusCodes, though often covered by Mvc
using IMSystem.Server.Core.Features.User.Commands; // Assuming RegisterUserCommand and UpdateMyProfileCommand are here
using System.Security.Claims; // Added for ClaimTypes
using System.Linq; // Added for .Any()

namespace IMSystem.Server.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly IMediator _mediator; // Added IMediator field

        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UsersController> logger, IMediator mediator) // Removed base(logger) call
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _mediator = mediator; // Assign IMediator
        }

        /// <summary>
        /// 注册一个新用户。
        /// </summary>
        /// <param name="request">用户注册请求数据。</param>
        /// <returns>成功注册后返回用户信息；如果失败，则返回错误信息。</returns>
        [HttpPost("register", Name = "RegisterUser")] // Added Name for CreatedAtRoute
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        // ApiConventions will handle other standard responses, Status409Conflict might be handled by HandleResult if command returns specific error
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
        {
            // Basic validation, more complex validation should be in the command handler or using FluentValidation
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: "Username and password are required.", instance: HttpContext.Request.Path));
            }

            // Assuming RegisterUserCommand exists in IMSystem.Server.Core.Features.User.Commands
            // And its constructor or properties match the request DTO
            var command = new RegisterUserCommand
            {
                Username = request.Username,
                Password = request.Password,
                Email = request.Email
            };

            var result = await _mediator.Send(command);

            // Assuming the command returns Result<UserDto>
            // If successful, return 201 Created with the new user's DTO and location header
            return HandleResult(result,
                userDto => CreatedAtRoute(nameof(GetUserById), new { userId = userDto.UserId }, userDto)); // Changed userDto.Id to userDto.UserId
        }

        /// <summary>
        /// 根据用户ID获取用户信息。
        /// </summary>
        /// <param name="userId">用户ID。</param>
        /// <returns>用户信息；如果未找到，则返回404。</returns>
        [HttpGet("{userId:guid}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        // 其他错误码由GetById约定和HandleResult覆盖
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                // Using ApiErrorFactory for direct bad request
                return BadRequest(ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: "User ID cannot be empty.", instance: HttpContext.Request.Path));
            }

            var query = new GetUserByIdQuery(userId); // Assuming GetUserByIdQuery exists and takes userId
            var result = await _mediator.Send(query);

            // Use HandleResult from BaseApiController to process the Result<UserDto?>
            // The second argument to HandleResult is a lambda that determines the OkObjectResult if successful.
            return HandleResult(result, userDto => userDto != null ? Ok(userDto) : NotFound(ApiErrorFactory.Create(ApiErrorCode.ResourceNotFound, detail: $"User with ID {userId} not found.", instance: HttpContext.Request.Path)));
        }

        /// <summary>
        /// 更新当前登录用户的个人资料。
        /// </summary>
        /// <param name="request">更新用户个人资料的请求数据。</param>
        /// <returns>成功则返回204 NoContent；失败则返回错误信息。</returns>
        [HttpPut("me/profile", Name = "UpdateMyProfile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        // ApiConventions will handle other standard responses
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: "Request body cannot be empty.", instance: HttpContext.Request.Path));
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized(ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: "User is not authenticated or user ID is invalid.", instance: HttpContext.Request.Path));
            }

            // Assuming UpdateUserProfileCommand exists in IMSystem.Server.Core.Features.User.Commands
            // And its constructor or properties match the request DTO and include the user ID.

            // Manual mapping for Gender enum as it might differ between Protocol and Domain
            IMSystem.Server.Domain.Enums.GenderType? domainGender = request.Gender.HasValue
                ? (IMSystem.Server.Domain.Enums.GenderType)request.Gender.Value
                : null;

            var command = new UpdateUserProfileCommand(
                currentUserId,
                request.Nickname,
                request.AvatarUrl,
                domainGender, // Use the mapped domainGender
                request.DateOfBirth,
                request.Bio,
                request.Street,
                request.City,
                request.StateOrProvince,
                request.Country,
                request.ZipCode
                // Assuming UpdateMyProfileCommand's constructor or properties match these.
                // If not, this part will need adjustment based on UpdateMyProfileCommand's actual definition.
            );

            var result = await _mediator.Send(command);

            // Assuming the command returns a simple Result (not Result<T>)
            return HandleResult(result, () => NoContent());
        }

        /// <summary>
        /// 更新当前已验证用户的自定义状态。
        /// </summary>
        /// <param name="request">包含新自定义状态的请求。</param>
        /// <returns>如果成功则无内容，否则返回错误响应。</returns>
        [HttpPut("me/status")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        // 其他错误码由Put约定和HandleResult覆盖
        public async Task<IActionResult> UpdateMyCustomStatus([FromBody] UpdateUserCustomStatusRequest request)
        {
            // TODO: 更新自定义状态逻辑需重构为直接调用仓储或服务
            return StatusCode(StatusCodes.Status501NotImplemented, "Update custom status not implemented.");
        }

        /// <summary>
        /// 获取当前登录用户的个人资料。
        /// </summary>
        /// <returns>当前用户的个人资料；如果用户未认证或未找到，则返回错误。</returns>
        [HttpGet("me/profile", Name = "GetMyProfile")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)] // In case user somehow doesn't exist
        // ApiConventions will handle other standard responses
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized(ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: "User is not authenticated or user ID is invalid.", instance: HttpContext.Request.Path));
            }

            var query = new GetUserByIdQuery(currentUserId); // Reusing GetUserByIdQuery
            var result = await _mediator.Send(query);

            return HandleResult(result, userDto => userDto != null ? Ok(userDto) : NotFound(ApiErrorFactory.Create(ApiErrorCode.ResourceNotFound, detail: "Current user profile not found.", instance: HttpContext.Request.Path)));
        }

        /// <summary>
        /// 搜索用户。
        /// </summary>
        /// <param name="query">搜索文本，用于匹配用户名、昵称或邮箱。</param>
        /// <returns>匹配的用户列表。</returns>
        [HttpGet("search", Name = "SearchUsers")]
        [Authorize] // Retained from original, as search usually requires auth
        [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        // ApiConventions will handle other standard responses
        public async Task<IActionResult> SearchUsers([FromQuery] SearchUsersRequest request) // Parameters from query string
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Keyword))
            {
                return BadRequest(ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: "Search keyword cannot be empty.", instance: HttpContext.Request.Path));
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized(ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: "User is not authenticated or user ID is invalid.", instance: HttpContext.Request.Path));
            }

            // Assuming SearchUsersQuery exists in IMSystem.Server.Core.Features.User.Queries
            // And its constructor or properties match the request DTO
            var query = new SearchUsersQuery(
                currentUserId,
                request.Keyword,
                request.Gender,
                request.PageNumber,
                request.PageSize
            );
            // It's good practice for the Query constructor or handler to set default values for pagination if not provided or invalid.

            var result = await _mediator.Send(query);

            // Assuming the query returns Result<PagedResult<UserSummaryDto>>
            return HandleResult(result, pagedResult => Ok(pagedResult));
        }

        /// <summary>
        /// 批量获取用户信息。
        /// </summary>
        /// <param name="request">包含用户外部ID列表的请求。</param>
        /// <returns>用户摘要信息列表；如果请求无效或发生错误，则返回错误信息。</returns>
        [HttpPost("batch-get", Name = "BatchGetUsers")] // Typically POST for requests with a body like a list of IDs
        [Authorize] // Retained Authorize as it's common for user-related endpoints
        [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        // ApiConventions will handle other standard responses
        public async Task<IActionResult> BatchGetUsers([FromBody] BatchGetUsersRequest request)
        {
            if (request == null || request.UserExternalIds == null || !request.UserExternalIds.Any())
            {
                return BadRequest(ApiErrorFactory.Create(ApiErrorCode.ValidationFailed, detail: "User ID list cannot be null or empty.", instance: HttpContext.Request.Path));
            }

            // Assuming BatchGetUsersQuery exists in IMSystem.Server.Core.Features.User.Queries
            // And its constructor or properties match the request DTO
            var query = new BatchGetUsersQuery(request.UserExternalIds); // Ensure BatchGetUsersQuery constructor matches

            var result = await _mediator.Send(query);

            // Assuming the query returns Result<IEnumerable<UserSummaryDto>>
            return HandleResult(result, userSummaries => Ok(userSummaries));
        }

        /// <summary>
        /// 停用当前已验证用户的帐户。
        /// </summary>
        /// <returns>如果成功则无内容，否则返回错误响应。</returns>
        [HttpDelete("me/account", Name = "DeactivateMyAccount")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        // ApiConventions will handle other standard responses
        public async Task<IActionResult> DeactivateMyAccount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var currentUserId))
            {
                return Unauthorized(ApiErrorFactory.Create(ApiErrorCode.AuthenticationFailed, detail: "User is not authenticated or user ID is invalid.", instance: HttpContext.Request.Path));
            }

            // Assuming DeactivateMyAccountCommand exists in IMSystem.Server.Core.Features.User.Commands
            var command = new DeactivateMyAccountCommand(currentUserId);

            var result = await _mediator.Send(command);

            // Assuming the command returns a simple Result (not Result<T>)
            return HandleResult(result, () => NoContent());
        }

        /// <summary>
        /// Gets the list of users blocked by the current user.
        /// </summary>
        /// <returns>A list of blocked users.</returns>
        [HttpGet("blocked")]
        [Authorize]
        [ProducesResponseType(typeof(Result<IEnumerable<BlockedUserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var blocked = await _unitOfWork.Friendships.FindAsync(
                f => (f.RequesterId == CurrentUserId || f.AddresseeId == CurrentUserId)
                    && f.Status == IMSystem.Server.Domain.Enums.FriendshipStatus.Blocked
                    && f.BlockedById == CurrentUserId);

            var dtos = blocked.Select(f =>
            {
                var otherUser = f.RequesterId == CurrentUserId ? f.Addressee : f.Requester;
                return new BlockedUserDto
                {
                    UserId = otherUser.Id,
                    Username = otherUser.Username,
                    BlockedAt = f.BlockedAt ?? DateTimeOffset.MinValue
                };
            }).ToList();

            return Ok(Result<IEnumerable<BlockedUserDto>>.Success(dtos));
        }
    }
}