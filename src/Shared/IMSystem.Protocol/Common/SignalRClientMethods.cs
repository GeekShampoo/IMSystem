namespace IMSystem.Protocol.Common
{
    /// <summary>
    /// SignalR客户端方法名常量定义，服务端与客户端均应统一引用。
    /// </summary>
    public static class SignalRClientMethods
    {
        // MessagingHub
        public const string ReceiveMessage = "ReceiveMessage";
        public const string MessageSentConfirmation = "MessageSentConfirmation";
        public const string EncryptedMessageSentConfirmation = "EncryptedMessageSentConfirmation";
        public const string ReceiveError = "ReceiveError";
        public const string ReceiveTypingNotification = "ReceiveTypingNotification";
        public const string ReceiveKeyExchangeOffer = "ReceiveKeyExchangeOffer";
        public const string ReceiveEncryptedMessage = "ReceiveEncryptedMessage";
        public const string ReceiveOfflineMessages = "ReceiveOfflineMessages";
        public const string MessageRecalled = "MessageRecalled";
        public const string ReceiveMessageReadNotification = "ReceiveMessageReadNotification";
        public const string ReceiveMessageEditedNotification = "ReceiveMessageEditedNotification";

        // PresenceHub
        public const string UserPresenceChanged = "UserPresenceChanged";

        // SignalingHub
        public const string CallInvited = "CallInvited";
        public const string CallAnswered = "CallAnswered";
        public const string CallRejected = "CallRejected";
        public const string CallHungup = "CallHungup";
        public const string SdpExchanged = "SdpExchanged";
        public const string IceCandidateExchanged = "IceCandidateExchanged";
        public const string CallStateChanged = "CallStateChanged";

        // NotificationHub
        public const string NewFriendRequest = "NewFriendRequest";
        public const string FriendRequestAccepted = "FriendRequestAccepted";
        public const string ReceiveFriendRequestRejected = "ReceiveFriendRequestRejected";
        public const string ReceiveFriendRemoved = "ReceiveFriendRemoved";
        public const string GroupCreated = "GroupCreated";
        public const string GroupDeleted = "GroupDeleted";
        public const string GroupDetailsUpdated = "GroupDetailsUpdated";
        public const string GroupInvitationAccepted = "GroupInvitationAccepted";
        public const string UserJoinedGroup = "UserJoinedGroup";
        public const string GroupMemberJoined = "GroupMemberJoined";
        public const string NewGroupInvitationNotification = "NewGroupInvitationNotification";
        public const string UserLeftGroup = "UserLeftGroup";
        public const string GroupMemberKicked = "GroupMemberKicked";
        public const string GroupOwnershipTransferred = "GroupOwnershipTransferred";
        public const string GroupMemberRoleUpdated = "GroupMemberRoleUpdated";
        public const string GroupAnnouncementUpdated = "GroupAnnouncementUpdated";
        public const string FriendGroupsReordered = "FriendGroupsReordered";
        public const string FriendGroupUpdated = "FriendGroupUpdated";
        public const string SendEmail = "SendEmail"; // Added for general notifications like email
        // ...如有新增客户端方法名，请在此补充
    }
}