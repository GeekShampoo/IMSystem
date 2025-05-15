// 错误码与错误描述统一定义，供全局引用，禁止局部定义。
// 迁移自各Feature目录，后续如需新增请在此集中维护。

using System;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Common
{
    /// <summary>
    /// 用户相关错误码
    /// </summary>
    public static class UserErrors
    {
        public static readonly Error CannotBlockSelf = new("User.CannotBlockSelf", "You cannot block yourself.");
        public static readonly Error UserToBlockNotFound = new("User.UserToBlockNotFound", "The user you are trying to block does not exist.");
        public static readonly Error UserAlreadyBlocked = new("User.UserAlreadyBlocked", "You have already blocked this user.");
        public static readonly Error BlockRelationshipNotFound = new("User.BlockRelationshipNotFound", "Block relationship not found.");
        public static readonly Error CannotUnblockSelf = new("User.CannotUnblockSelf", "You cannot unblock yourself.");
        public static readonly Error UserToUnblockNotFound = new("User.UserToUnblockNotFound", "The user you are trying to unblock does not exist.");
        public static readonly Error NotFound = new("User.NotFound", "用户不存在。");
        public static readonly Error CustomStatusUpdateError = new("User.CustomStatus.UpdateError", "更新用户状态时发生错误。");

        // 保留原始方法名以便兼容现有代码
        public static string UserNotFoundDescription(Guid userId) =>
            $"未找到ID为 '{userId}' 的用户。"; // User.NotFound
    }

    /// <summary>
    /// 群组相关错误码
    /// </summary>
    public static class GroupErrors
    {
        public static readonly Error NotFound = new("Group.NotFound", "群组不存在。");
        public static readonly Error AccessDenied = new("Group.AccessDenied", "您没有权限访问此群组。");
        public static readonly Error PermissionDenied = new("Group.PermissionDenied", "您没有足够权限执行此操作。");
        public static readonly Error ActorNotMember = new("Group.ActorNotMember", "操作者不是群组成员，无权执行此操作。");
        public static readonly Error TargetNotMember = new("Group.TargetNotMember", "目标用户不是群组成员。");
        public static readonly Error CannotKickSelf = new("Group.Kick.CannotKickSelf", "您不能将自己踢出群组。请使用退群功能。");
        public static readonly Error CannotKickOwner = new("Group.Kick.CannotKickOwner", "不能将群主踢出群组。");
        public static readonly Error KickPermissionDenied = new("Group.Kick.PermissionDenied", "您的权限不足以踢出该成员。");
        public static readonly Error DisbandUnexpectedError = new("Group.Disband.UnexpectedError", "解散群组时发生意外错误。");
        public static readonly Error TransferOwnershipInvalidOperation = new("Group.TransferOwnership.InvalidOperation", "转让群主权限操作无效。");
        public static readonly Error TransferOwnershipUnexpectedError = new("Group.TransferOwnership.UnexpectedError", "转让群主权限时发生意外错误。");

        // 保留原始方法名以便兼容现有代码
        public static string GroupAlreadyExistsDescription(string name, Guid ownerId) =>
            $"用户 '{ownerId}' 已拥有名为 '{name}' 的群组。"; // Group.AlreadyExists

        public static Error GroupNotFound(Guid groupId) =>
            new("Group.NotFound", $"未找到ID为 '{groupId}' 的群组。");
    }

    /// <summary>
    /// 好友关系相关错误码
    /// </summary>
    public static class FriendshipErrors
    {
        public static readonly Error NotFound = new("Friendship.NotFound", "好友关系不存在。");
        public static readonly Error NotAccepted = new("Friendship.NotAccepted", "只有已接受的好友才能添加到分组。");
        public static readonly Error UnexpectedError = new("FriendRequest.UnexpectedError", "处理好友请求时发生意外错误。");

        // 保留原始方法名以便兼容现有代码
        public static string NotFriendsDescription(Guid userId1, Guid userId2) =>
            $"用户 '{userId1}' 和用户 '{userId2}' 不是好友关系。"; // Friendship.NotFriends

        public static Error NotFriends(Guid userId1, Guid userId2) =>
            new("Friendship.NotFriends", $"用户 '{userId1}' 和用户 '{userId2}' 不是好友关系。");
    }

    /// <summary>
    /// 消息相关错误码
    /// </summary>
    public static class MessageErrors
    {
        public static readonly Error BlockedRelationshipExists = new("Message.BlockedRelationshipExists", "Cannot send message due to a block relationship.");
        public static readonly Error NotFound = new("Message.NotFound", "消息不存在。");
        public static readonly Error AccessDenied = new("Message.Read.AccessDenied", "无权操作此消息。");
        public static readonly Error InvalidParams = new("Message.Read.InvalidParams", "无效的命令参数。");
        public static readonly Error RecallFailed = new("Message.Recall.Failed", "撤回消息失败，可能超出撤回时间限制或您不是消息发送者。");
        public static readonly Error RecallUnexpectedError = new("Message.Recall.UnexpectedError", "撤回消息时发生意外错误。");
        public static readonly Error EditFailed = new("Message.EditFailed", "编辑消息失败。");
        public static readonly Error SendUnexpectedError = new("Message.Send.UnexpectedError", "发送消息时发生意外错误。");
    }
    
    /// <summary>
    /// 好友分组相关错误码
    /// </summary>
    public static class FriendGroupErrors
    {
        public static readonly Error NotFoundGeneric = new("FriendGroup.NotFound", "好友分组不存在。");
        public static readonly Error AccessDenied = new("FriendGroup.AccessDenied", "您无权修改此好友分组。");
        public static readonly Error NameConflictGeneric = new("FriendGroup.NameConflict", "您已拥有同名的好友分组。");
        public static readonly Error OrderConflictGeneric = new("FriendGroup.OrderConflict", "您已拥有相同排序值的好友分组。");
        public static readonly Error ReservedName = new("FriendGroup.ReservedName", "此分组名称是保留名称，不允许使用。");
        public static readonly Error CannotModifyDefaultName = new("FriendGroup.CannotModifyDefaultName", "默认分组名称不能被修改。");
        public static readonly Error CannotModifyDefaultOrder = new("FriendGroup.CannotModifyDefaultOrder", "默认分组排序不能被修改。");
        public static readonly Error CannotDeleteDefault = new("FriendGroup.CannotDeleteDefault", "默认分组不能被删除。");
        public static readonly Error DefaultGroupMissing = new("FriendGroup.DefaultGroupMissing", "未找到用户的默认好友分组，无法继续操作。");
        
        public static Error NotFound(Guid groupId) =>
            new("FriendGroup.NotFound", $"未找到ID为 '{groupId}' 的好友分组。");
            
        public static Error NameConflict(string name) =>
            new("FriendGroup.NameConflict", $"您已拥有一个名为 '{name}' 的分组。请使用其他名称。");
            
        public static Error OrderConflict(int order) =>
            new("FriendGroup.OrderConflict", $"您已拥有一个排序值为 {order} 的分组。请选择其他排序值。");
    }
    
    /// <summary>
    /// 文件相关错误码
    /// </summary>
    public static class FileErrors
    {
        public static readonly Error NotFound = new("File.NotFound", "找不到指定的文件记录。");
        public static readonly Error AccessDenied = new("File.AccessDenied", "您没有权限访问此文件记录。");
        public static readonly Error PresignedUrlError = new("File.PresignedUrlError", "无法生成文件上传URL，请稍后重试。");
        public static readonly Error NotConfirmed = new("File.NotConfirmed", "文件尚未确认，无法访问。");
        public static readonly Error InvalidMetadata = new("File.InvalidMetadata", "文件元数据信息不完整，无法处理。");
        public static readonly Error StorageError = new("File.StorageError", "保存文件信息失败。");
        public static readonly Error DbError = new("File.DbError", "删除文件元数据失败。");
        public static readonly Error PhysicalDeleteError = new("File.PhysicalDeleteError", "文件记录已成功删除，但物理文件清理时遇到问题。请联系管理员。");
        public static readonly Error UnexpectedError = new("File.UnexpectedError", "处理文件时发生内部错误。");
    }
    
    /// <summary>
    /// 认证相关错误码
    /// </summary>
    public static class AuthErrors
    {
        public static readonly Error UserNotFound = new("Auth.UserNotFound", "用户名或密码错误。");
        public static readonly Error AccountDeactivated = new("Auth.AccountDeactivated", "您的账户已被停用。");
        public static readonly Error InvalidPassword = new("Auth.InvalidPassword", "用户名或密码错误。");
    }
    
    /// <summary>
    /// 信令相关错误码
    /// </summary>
    public static class SignalingErrors
    {
        public static readonly Error OperationFailed = new("Signaling.OperationFailed", "信令操作失败。");
        public static readonly Error InviteError = new("Signaling.Invite.Error", "信令邀请发送失败。");
        
        public static Error UserNotFound(Guid userId) =>
            new("Signaling.UserNotFound", $"找不到ID为 '{userId}' 的用户。");
    }
}