using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Protocol.DTOs.Requests.Groups;
using IMSystem.Protocol.DTOs.Notifications.Groups; // Added for SignalR notifications
using CommunityToolkit.Mvvm.Messaging; // Assuming using CommunityToolkit.Mvvm for IMessenger
using Microsoft.Extensions.Logging; // Added for logging

namespace IMSystem.Client.Core.Services
{
   /// <summary>
   /// Provides services for managing groups.
   /// </summary>
   public class GroupService : IGroupService,
                               IRecipient<GroupCreatedNotificationDto>,      // Handle GroupCreated
                               IRecipient<GroupDeletedNotificationDto>,      // Handle GroupDeleted
                               IRecipient<UserJoinedGroupNotificationDto>,   // Handle UserJoinedGroup
                               IRecipient<UserLeftGroupNotificationDto>,     // Handle UserLeftGroup
                               IRecipient<GroupDetailsUpdatedNotificationDto>,// Handle GroupDetailsUpdated
                               IRecipient<GroupMemberKickedNotificationDto>  // Handle GroupMemberKicked
                               // Add other relevant notifications if needed
   {
       private readonly IApiService _apiService;
       private readonly IDatabaseService _databaseService;
       private readonly IAuthService _authService; // 添加IAuthService依赖
       private readonly ILogger<GroupService> _logger;
       private readonly IMessenger _messenger; // For receiving SignalR notifications

       /// <summary>
       /// Initializes a new instance of the <see cref="GroupService"/> class.
       /// </summary>
       /// <param name="apiService">The API service for making HTTP requests.</param>
       /// <param name="databaseService">The database service for local caching.</param>
       /// <param name="authService">The authentication service for user information.</param>
       /// <param name="logger">The logger.</param>
       /// <param name="messenger">The messenger for SignalR notifications.</param>
       public GroupService(
           IApiService apiService, 
           IDatabaseService databaseService, 
           IAuthService authService, 
           ILogger<GroupService> logger, 
           IMessenger messenger)
       {
           _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
           _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
           _authService = authService ?? throw new ArgumentNullException(nameof(authService)); 
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
           _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

           // Register to receive messages
           _messenger.RegisterAll(this);
       }

       /// <inheritdoc />
       public async Task<Result<PagedResult<GroupDto>>> GetUserGroupsAsync(int pageNumber, int pageSize)
       {
           try
           {
               // 获取当前登录用户的ID
               var currentUserId = _authService.CurrentUserId;
               if (!currentUserId.HasValue)
               {
                   _logger.LogWarning("尝试获取用户群组，但用户未认证");
                   return Result<PagedResult<GroupDto>>.Failure(new Error("Auth.Required", "用户未认证"));
               }

               // 从本地数据库获取当前用户加入的群组
               var localGroups = await _databaseService.GetUserJoinedGroupsAsync(currentUserId.Value);
               if (localGroups.Any()) // 如果本地有缓存数据
               {
                   _logger.LogInformation("从本地缓存加载用户 {UserId} 的群组", currentUserId.Value);
                   // 对本地数据进行分页
                   var totalCount = localGroups.Count;
                   var pagedItems = localGroups.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                   var pagedLocalGroups = PagedResult<GroupDto>.Success(pagedItems, totalCount, pageNumber, pageSize);
                   return Result<PagedResult<GroupDto>>.Success(pagedLocalGroups);
               }

               // 如果本地没有数据或需要刷新，从API获取
               _logger.LogInformation("从API获取用户 {UserId} 的群组", currentUserId.Value);
               var endpoint = $"/api/Groups?pageNumber={pageNumber}&pageSize={pageSize}";
               var apiResult = await _apiService.GetAsync<PagedResult<GroupDto>>(endpoint);

               if (apiResult != null && apiResult.Items.Any())
               {
                   await _databaseService.SaveGroupsAsync(apiResult.Items);
                   _logger.LogInformation("将获取的用户群组保存到本地缓存");
               }
               return Result<PagedResult<GroupDto>>.Success(apiResult);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "获取用户群组时出错");
               return Result<PagedResult<GroupDto>>.Failure(new Error("GroupServiceError", $"获取用户群组时出错: {ex.Message}"));
           }
       }

       /// <inheritdoc />
       public async Task<Result<GroupDto>> GetGroupDetailsAsync(Guid groupId, int membersPageNumber, int membersPageSize)
       {
           try
           {
               // Attempt to load group details and members from local database
               var localGroup = await _databaseService.GetGroupAsync(groupId);
               List<GroupMemberDto> localMembers = new List<GroupMemberDto>();
               if (localGroup != null)
               {
                    localMembers = await _databaseService.GetGroupMembersAsync(groupId);
                    // Assuming GroupDto needs its Members property populated
                    // Use the static factory method for PagedResult
                    localGroup.Members = PagedResult<GroupMemberDto>.Success(localMembers, localMembers.Count, 1, localMembers.Count > 0 ? localMembers.Count : 1);
                   _logger.LogInformation("Loaded group details for {GroupId} from local cache.", groupId);
                   return Result<GroupDto>.Success(localGroup);
               }

               // If not in cache or needs refresh, fetch from API
               _logger.LogInformation("Fetching group details for {GroupId} from API.", groupId);
               var endpoint = $"/api/Groups/{groupId}?membersPageNumber={membersPageNumber}&membersPageSize={membersPageSize}";
               var apiGroupDetails = await _apiService.GetAsync<GroupDto>(endpoint);

               if (apiGroupDetails != null)
               {
                   await _databaseService.SaveGroupAsync(apiGroupDetails);
                   if (apiGroupDetails.Members != null && apiGroupDetails.Members.Items.Any())
                   {
                       await _databaseService.SaveGroupMembersAsync(groupId, apiGroupDetails.Members.Items);
                   }
                   _logger.LogInformation("Saved fetched group details for {GroupId} to local cache.", groupId);
               }
               return Result<GroupDto>.Success(apiGroupDetails);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error fetching group details for group {GroupId}.", groupId);
               return Result<GroupDto>.Failure(new Error("GroupServiceError", $"Error fetching group details for group {groupId}: {ex.Message}"));
           }
       }

       // Handle SignalR Notifications
       public async void Receive(GroupCreatedNotificationDto message)
       {
           _logger.LogInformation("Received GroupCreatedNotification for group {GroupId}. Updating cache.", message.GroupId);
           // Construct GroupDto from notification
           var groupToSave = new GroupDto
           {
               Id = message.GroupId,
               Name = message.GroupName,
               CreatedAt = message.CreatedAt,
               OwnerId = message.CreatorUserId // Assuming CreatorUserId maps to OwnerId
               // Populate other fields if available in notification and needed for GroupDto
           };
           await _databaseService.SaveGroupAsync(groupToSave);
       }

       public async void Receive(GroupDeletedNotificationDto message)
       {
           _logger.LogInformation("Received GroupDeletedNotification for group {GroupId}. Updating cache.", message.GroupId);
           await _databaseService.DeleteGroupAsync(message.GroupId);
       }

       public async void Receive(UserJoinedGroupNotificationDto message)
       {
           _logger.LogInformation("Received UserJoinedGroupNotification for user {UserId} in group {GroupId}. Updating cache.", message.UserId, message.GroupId);
           // Construct GroupMemberDto from notification
           var memberToSave = new GroupMemberDto
           {
               UserId = message.UserId,
               Username = message.Username,
               AvatarUrl = message.UserAvatarUrl,
               Role = message.Role,
               JoinedAt = message.JoinedAt
               // Nickname and NicknameInGroup might need to be fetched or set to default
           };
           await _databaseService.SaveGroupMemberAsync(message.GroupId, memberToSave);
       }

       public async void Receive(UserLeftGroupNotificationDto message)
       {
           _logger.LogInformation("Received UserLeftGroupNotification for user {UserId} from group {GroupId}. Updating cache.", message.UserId, message.GroupId);
           await _databaseService.DeleteGroupMemberAsync(message.GroupId, message.UserId);
       }

       public async void Receive(GroupDetailsUpdatedNotificationDto message)
       {
           _logger.LogInformation("Received GroupDetailsUpdatedNotification for group {GroupId}. Updating cache.", message.GroupId);
           // Construct GroupDto from notification for update
           // It's better to fetch the existing group and update its properties,
           // or ensure SaveGroupAsync handles partial updates correctly (INSERT OR REPLACE does this).
           var groupToUpdate = await _databaseService.GetGroupAsync(message.GroupId);
           if (groupToUpdate != null)
           {
               groupToUpdate.Name = message.GroupName;
               groupToUpdate.Description = message.Description;
               groupToUpdate.AvatarUrl = message.AvatarUrl;
               // GroupDto does not have UpdatedAt, CreatedAt is set on creation
               // If an UpdatedAt field is added to GroupDto and the local DB table, it can be set here.
               // For now, we rely on the API to manage UpdatedAt if it's a server-side concept.
               // The local SaveGroupAsync will just save the current state.
               await _databaseService.SaveGroupAsync(groupToUpdate);
           }
           else
           {
               // If group doesn't exist locally, create it
                var newGroup = new GroupDto
               {
                   Id = message.GroupId,
                   Name = message.GroupName,
                   Description = message.Description,
                   AvatarUrl = message.AvatarUrl,
                   CreatedAt = message.UpdatedAt, // Assuming UpdatedAt from notification can be used as CreatedAt if new
                   // OwnerId would need to come from the notification if available, or be set to a default/unknown
               };
               await _databaseService.SaveGroupAsync(newGroup);
               _logger.LogWarning("Group {GroupId} not found locally for update, created instead.", message.GroupId);
           }
           // This notification DTO does not contain member information.
           // If member updates are tied to group detail updates, a separate mechanism or richer DTO is needed.
       }

       public async void Receive(GroupMemberKickedNotificationDto message)
       {
           _logger.LogInformation("Received GroupMemberKickedNotification for user {KickedUserId} from group {GroupId}. Updating cache.", message.KickedUserId, message.GroupId);
           await _databaseService.DeleteGroupMemberAsync(message.GroupId, message.KickedUserId);
       }

       // It's good practice to unregister from messages when the service is disposed,
       // if the service lifecycle is managed and it implements IDisposable.
       // For simplicity, this is omitted here but consider for a production app.
       /// <summary>
       /// Handles API responses and standardizes the result.
       /// </summary>
       /// <typeparam name="T">The type of the result.</typeparam>
       /// <param name="apiCall">The API call to execute.</param>
       /// <returns>A task that represents the asynchronous operation. The task result contains the standardized result.</returns>
       private async Task<Result<T>> HandleApiResponseAsync<T>(Func<Task<T>> apiCall)
       {
           try
           {
               var response = await apiCall();
               return Result<T>.Success(response);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "An error occurred while processing the API call.");
               return Result<T>.Failure("ApiError", $"An error occurred while processing the API call: {ex.Message}");
           }
       }

       /// <inheritdoc />
       public async Task<Result<Guid>> CreateGroupAsync(CreateGroupRequest request)
       {
           try
           {
               var response = await _apiService.PostAsync<CreateGroupRequest, CreateGroupResponseDtoWrapper>("/api/Groups", request);
               return response != null
                   ? Result<Guid>.Success(response.GroupId)
                   : Result<Guid>.Failure("Group.Create.NoResponse", "Failed to create group, no valid response from server.");
           }
           catch (ApiException ex)
           {
               return Result<Guid>.Failure(ex.Error.ErrorCode ?? "ApiError", ex.Error.Title ?? ex.Message);
           }
           catch (Exception ex)
           {
               return Result<Guid>.Failure("UnexpectedError", ex.Message);
           }
       }

       /// <inheritdoc />
       public async Task<Result> UpdateGroupDetailsAsync(Guid groupId, UpdateGroupDetailsRequest request)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PutAsync($"/api/Groups/{groupId}", request);
               return Result.Success();
           });
       }

       private class CreateGroupResponseDtoWrapper
       {
           public Guid GroupId { get; set; }
       }
       /// <inheritdoc />
       public async Task<Result> AcceptGroupInvitationAsync(Guid invitationId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync<object, object>($"/api/Groups/Invitations/{invitationId}/Accept", null);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> RejectGroupInvitationAsync(Guid invitationId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync<object, object>($"/api/Groups/Invitations/{invitationId}/Reject", null);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result<IEnumerable<GroupInvitationDto>>> GetPendingGroupInvitationsAsync()
       {
           return await HandleApiResponseAsync(async () =>
           {
               return await _apiService.GetAsync<IEnumerable<GroupInvitationDto>>("/api/Groups/Invitations/Pending");
           });
       }

       /// <inheritdoc />
       public async Task<Result<Guid>> InviteUserToGroupAsync(Guid groupId, InviteUserToGroupRequest request)
       {
           return await HandleApiResponseAsync(async () =>
           {
               var response = await _apiService.PostAsync<InviteUserToGroupRequest, Guid>($"/api/Groups/{groupId}/Invitations", request);
               return response;
           });
       }

       /// <inheritdoc />
       public async Task<Result> LeaveGroupAsync(Guid groupId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync<object, object>($"/api/Groups/{groupId}/Leave", null);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> TransferGroupOwnershipAsync(Guid groupId, TransferGroupOwnershipRequest request)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync($"/api/Groups/{groupId}/TransferOwnership", request);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> PromoteMemberToAdminAsync(Guid groupId, Guid memberUserId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync<object, object>($"/api/Groups/{groupId}/Members/{memberUserId}/Promote", null);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> DemoteAdminToMemberAsync(Guid groupId, Guid memberUserId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync<object, object>($"/api/Groups/{groupId}/Members/{memberUserId}/Demote", null);
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> DisbandGroupAsync(Guid groupId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.DeleteAsync($"/api/Groups/{groupId}");
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> CancelGroupInvitationAsync(Guid invitationId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.DeleteAsync($"/api/Groups/Invitations/{invitationId}");
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result<IEnumerable<GroupInvitationDto>>> GetSentGroupInvitationsAsync(Guid groupId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               return await _apiService.GetAsync<IEnumerable<GroupInvitationDto>>($"/api/Groups/{groupId}/Invitations/Sent");
           });
       }

       /// <inheritdoc />
       public async Task<Result> KickGroupMemberAsync(Guid groupId, Guid memberUserId)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.DeleteAsync($"/api/Groups/{groupId}/Members/{memberUserId}");
               return Result.Success();
           });
       }

       /// <inheritdoc />
       public async Task<Result> SetGroupAnnouncementAsync(Guid groupId, SetGroupAnnouncementRequest request)
       {
           return await HandleApiResponseAsync(async () =>
           {
               await _apiService.PostAsync($"/api/Groups/{groupId}/Announcement", request);
               return Result.Success();
           });
       }
   }
}