using System;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services
{
    public interface IPermissionService
    {
        Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
        Task<bool> CanUserManageGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default); // 例如，检查是否为群主或管理员
        Task<bool> AreUsersFriendsAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default); // 检查是否为已确认的好友
        // Task<bool> CanUserSendMessageToUserAsync(Guid senderId, Guid receiverId, CancellationToken cancellationToken = default);
        // Task<bool> CanUserAccessFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default);
    }
}