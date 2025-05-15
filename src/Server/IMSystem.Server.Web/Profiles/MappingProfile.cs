using AutoMapper;
using IMSystem.Protocol.DTOs.Messages; // For SendMessageDto and MessageDto
using IMSystem.Protocol.DTOs.Requests.Messages; // Added for MarkMessageAsReadRequest
using IMSystem.Protocol.DTOs.Requests.Friends;
using IMSystem.Protocol.DTOs.Responses.Auth;
using IMSystem.Protocol.DTOs.Responses.Friends;
using IMSystem.Protocol.DTOs.Responses.FriendGroups; // For FriendGroupDto
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.DTOs.Requests.User;
using IMSystem.Protocol.DTOs.Requests.FriendGroups; // For CreateFriendGroupRequest
using IMSystem.Server.Core.Features.Authentication.Commands; // For LoginCommand
using IMSystem.Server.Core.Features.User.Commands;
using IMSystem.Server.Core.Features.Friends.Commands;
using IMSystem.Server.Core.Features.FriendGroups.Commands; // For friend group commands
using IMSystem.Server.Core.Features.Messages.Commands; // For SendMessageCommand
using IMSystem.Server.Core.Features.Groups.Commands; // For CreateGroupCommand
using IMSystem.Protocol.DTOs.Requests.Groups; // For CreateGroupRequest
using IMSystem.Protocol.DTOs.Responses.Groups; // For GroupDto, GroupMemberDto
using IMSystem.Protocol.DTOs.Requests.Files;  // For File DTOs
using IMSystem.Protocol.DTOs.Responses.Files; // For File DTOs
using IMSystem.Server.Core.Features.Files.Commands; // For File Commands
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums; // Added for domain enums like MessageRecipientType
using IMSystem.Protocol.Enums; // For ProtocolGender

namespace IMSystem.Server.Web.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Entity to DTO
            CreateMap<User, LoginResponse>(); // Assuming LoginResponse fields match User or are customized
            CreateMap<User, FriendDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id)) // UserId in FriendDto is Guid
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom<UserAvatarResolver>())
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                .ForMember(dest => dest.CustomStatus, opt => opt.MapFrom(src => src.CustomStatus))
                .ForMember(dest => dest.LastSeenAt, opt => opt.MapFrom(src => src.LastSeenAt))
                .ForMember(dest => dest.RemarkName, opt => opt.Ignore()); // RemarkName is set manually in GetFriendsQueryHandler
                // FriendshipId and Status are set manually in GetFriendsQueryHandler

            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nickname, opt => opt.MapFrom<UserNicknameResolver>()) // Fallback to Username if no profile or nickname
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom<UserAvatarResolver>());
                // Username, Email, CreatedAt, IsOnline, CustomStatus will be auto-mapped if names and types match.

            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nickname, opt => opt.MapFrom<UserNicknameResolver>())
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom<UserAvatarResolver>())
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                .ForMember(dest => dest.CustomStatus, opt => opt.MapFrom(src => src.CustomStatus))
                // .ForMember(dest => dest.RemarkName, opt => opt.Ignore()); // UserSummaryDto无RemarkName属性，移除
;

            CreateMap<Friendship, FriendRequestDto>()
                .ForMember(dest => dest.RequestId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RequesterId, opt => opt.MapFrom(src => src.Requester.Id.ToString())) // Assumes Requester is loaded
                .ForMember(dest => dest.RequesterUsername, opt => opt.MapFrom(src => src.Requester.Username)) // Assumes Requester is loaded
                .ForMember(dest => dest.RequesterProfilePictureUrl, opt => opt.MapFrom(src => src.Requester.Profile != null ? src.Requester.Profile.AvatarUrl : null)) // Assumes Requester and its Profile are loaded
                .ForMember(dest => dest.RequestedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapDomainFriendStatusToProtocol(src.Status)));

            CreateMap<FriendGroup, FriendGroupDto>()
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.CreatedBy.ToString())) // UserId is CreatedBy in FriendGroup
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault)); // Added IsDefault mapping
            
            CreateMap<Group, GroupDto>(); // Basic mapping, Members might need custom handling if populated
            CreateMap<GroupMember, GroupMemberDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => MapDomainGroupMemberRoleToProtocol(src.Role)))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.CreatedAt)) // JoinedAt is CreatedAt in GroupMember
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username)) // Assumes User is loaded
                .ForMember(dest => dest.Nickname, opt => opt.MapFrom((src, dest, destMember, context) => new UserNicknameResolver().Resolve(src.User, dest, destMember, context))) // Assumes User and Profile are loaded
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom((src, dest, destMember, context) => new UserAvatarResolver().Resolve(src.User, dest, destMember, context))); // Assumes User and Profile are loaded

            CreateMap<GroupInvitation, GroupInvitationDto>()
                .ForMember(dest => dest.InvitationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.GroupId))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Group != null ? src.Group.Name : string.Empty)) // Assumes Group is loaded
                .ForMember(dest => dest.GroupAvatarUrl, opt => opt.MapFrom(src => src.Group != null ? src.Group.AvatarUrl : null)) // Assumes Group is loaded
                .ForMember(dest => dest.InviterId, opt => opt.MapFrom(src => src.InviterId))
                .ForMember(dest => dest.InviterUsername, opt => opt.MapFrom(src => src.Inviter != null ? src.Inviter.Username : string.Empty)) // Assumes Inviter is loaded
                .ForMember(dest => dest.InviterNickname, opt => opt.MapFrom(src => src.Inviter != null && src.Inviter.Profile != null ? src.Inviter.Profile.Nickname : (src.Inviter != null ? src.Inviter.Username : null))) // Assumes Inviter and Profile are loaded
                .ForMember(dest => dest.InviterAvatarUrl, opt => opt.MapFrom(src => src.Inviter != null && src.Inviter.Profile != null ? src.Inviter.Profile.AvatarUrl : null)) // Assumes Inviter and Profile are loaded
                .ForMember(dest => dest.InvitedUserId, opt => opt.MapFrom(src => src.InvitedUserId))
                .ForMember(dest => dest.InvitedUsername, opt => opt.MapFrom(src => src.InvitedUser != null ? src.InvitedUser.Username : string.Empty)) // Assumes InvitedUser is loaded
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapDomainGroupInvitationStatusToProtocol(src.Status)))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt));


            // DTO to Command/Query
            CreateMap<RegisterUserRequest, RegisterUserCommand>();
            CreateMap<UpdateUserProfileRequest, UpdateUserProfileCommand>()
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => MapProtocolGenderToDomain(src.Gender)));
            CreateMap<CreateGroupRequest, CreateGroupCommand>()
                .ForMember(dest => dest.CreatorUserId, opt => opt.Ignore()); // CreatorUserId will be set from HttpContext

            CreateMap<SendMessageDto, SendMessageCommand>()
                .ForMember(dest => dest.SenderId, opt => opt.Ignore()); // SenderId will be set manually from Hub context
            
            // Message Entity to MessageDto
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.MessageId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.CreatedBy.ToString())) // SenderId is CreatedBy in Message
                .ForMember(dest => dest.RecipientId, opt => opt.MapFrom(src => src.RecipientId.ToString()))
                .ForMember(dest => dest.RecipientType, opt => opt.MapFrom(src => MapDomainMessageRecipientTypeToProtocol(src.RecipientType)))
                .ForMember(dest => dest.MessageType, opt => opt.MapFrom(src => MapDomainMessageTypeToProtocol(src.Type)))
                .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.CreatedAt)) // SentAt is CreatedAt in Message
                .ForMember(dest => dest.SenderUsername, opt => opt.MapFrom((src, dest, destMember, context) => new UserNicknameResolver().Resolve(src.Sender, dest, destMember, context)))
                .ForMember(dest => dest.SenderAvatarUrl, opt => opt.MapFrom((src, dest, destMember, context) => new UserAvatarResolver().Resolve(src.Sender, dest, destMember, context)))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src =>
                    src.RecipientType == MessageRecipientType.Group && src.RecipientGroup != null ?
                        src.RecipientGroup.Name
                        : null))
                .ForMember(dest => dest.ReadCount, opt => opt.Ignore()); // ReadCount is populated by the handler

            // File Upload Mappings
            CreateMap<RequestFileUploadRequest, RequestFileUploadCommand>()
                .ForMember(dest => dest.RequesterId, opt => opt.Ignore()); // RequesterId will be set from HttpContext in Controller

            CreateMap<ConfirmFileUploadRequest, ConfirmFileUploadCommand>()
                .ForMember(dest => dest.ConfirmerId, opt => opt.Ignore()); // ConfirmerId will be set from HttpContext in Controller

            CreateMap<FileMetadata, FileMetadataDto>()
                .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.CreatedAt)) // UploadedAt is CreatedAt in FileMetadata
                .ForMember(dest => dest.LastModifiedAt, opt => opt.MapFrom(src => src.LastModifiedAt))
                .ForMember(dest => dest.UploaderId, opt => opt.MapFrom(src => src.CreatedBy)) // UploaderId is CreatedBy
                .ForMember(dest => dest.UploaderUsername, opt => opt.MapFrom(src => src.Uploader != null ? src.Uploader.Username : null)); // Assumes Uploader is loaded
        }

        private static GenderType? MapProtocolGenderToDomain(ProtocolGender? protocolGender)
        {
            if (!protocolGender.HasValue)
            {
                return null;
            }

            return protocolGender.Value switch
            {
                ProtocolGender.Male => GenderType.Male,
                ProtocolGender.Female => GenderType.Female,
                ProtocolGender.Other => GenderType.Other,
                ProtocolGender.Unspecified => GenderType.Unknown,
                ProtocolGender.PreferNotToSay => GenderType.Unknown,
                _ => GenderType.Unknown // Default case, though all enum values should be handled
            };
        }

        // FriendshipStatus <-> ProtocolFriendStatus
        private static ProtocolFriendStatus MapDomainFriendStatusToProtocol(FriendshipStatus status)
        {
            return status switch
            {
                FriendshipStatus.Accepted => ProtocolFriendStatus.Friends,
                FriendshipStatus.Pending => ProtocolFriendStatus.PendingOutgoing,
                FriendshipStatus.Declined => ProtocolFriendStatus.None,
                FriendshipStatus.Blocked => ProtocolFriendStatus.BlockedBySelf,
                _ => ProtocolFriendStatus.None
            };
        }

        // GroupInvitationStatus <-> ProtocolGroupInvitationStatus
        private static ProtocolGroupInvitationStatus MapDomainGroupInvitationStatusToProtocol(GroupInvitationStatus status)
        {
            return status switch
            {
                GroupInvitationStatus.Pending => ProtocolGroupInvitationStatus.Pending,
                GroupInvitationStatus.Accepted => ProtocolGroupInvitationStatus.Accepted,
                GroupInvitationStatus.Rejected => ProtocolGroupInvitationStatus.Rejected,
                GroupInvitationStatus.Cancelled => ProtocolGroupInvitationStatus.Cancelled,
                GroupInvitationStatus.Expired => ProtocolGroupInvitationStatus.Expired,
                _ => ProtocolGroupInvitationStatus.Pending
            };
        }

        // GroupMemberRole <-> ProtocolGroupUserRole
        private static ProtocolGroupUserRole MapDomainGroupMemberRoleToProtocol(GroupMemberRole role)
        {
            return role switch
            {
                GroupMemberRole.Owner => ProtocolGroupUserRole.Owner,
                GroupMemberRole.Admin => ProtocolGroupUserRole.Admin,
                GroupMemberRole.Member => ProtocolGroupUserRole.Member,
                _ => ProtocolGroupUserRole.Member
            };
        }

        // MessageRecipientType <-> ProtocolMessageRecipientType
        private static ProtocolMessageRecipientType MapDomainMessageRecipientTypeToProtocol(MessageRecipientType type)
        {
            return type switch
            {
                MessageRecipientType.User => ProtocolMessageRecipientType.User,
                MessageRecipientType.Group => ProtocolMessageRecipientType.Group,
                _ => ProtocolMessageRecipientType.User
            };
        }

        // MessageType <-> ProtocolMessageType
        private static ProtocolMessageType MapDomainMessageTypeToProtocol(MessageType type)
        {
            return type switch
            {
                MessageType.Text => ProtocolMessageType.Text,
                MessageType.Image => ProtocolMessageType.Image,
                MessageType.File => ProtocolMessageType.File,
                MessageType.Audio => ProtocolMessageType.Audio,
                MessageType.Video => ProtocolMessageType.Video,
                MessageType.System => ProtocolMessageType.System,
                // EncryptedText、Call、Recalled等可根据业务需求扩展
                _ => ProtocolMessageType.Text
            };
        }
    }

    public class UserAvatarResolver : IValueResolver<User, object, string?>
    {
        public string? Resolve(User source, object destination, string? destMember, ResolutionContext context)
        {
            return source?.Profile?.AvatarUrl;
        }
    }

    public class UserNicknameResolver : IValueResolver<User, object, string?>
    {
        public string? Resolve(User source, object destination, string? destMember, ResolutionContext context)
        {
            if (source == null) return null;
            return !string.IsNullOrEmpty(source.Profile?.Nickname) ? source.Profile.Nickname : source.Username;
        }
    }
}