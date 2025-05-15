using IMSystem.Client.Core.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Protocol.DTOs.Messages; // General messages
using IMSystem.Protocol.DTOs.Notifications.Friends;
using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Protocol.DTOs.Notifications; // General notifications namespace
// using IMSystem.Protocol.DTOs.Notifications.Presence; // Will be more specific if needed
// using IMSystem.Protocol.DTOs.Notifications.Signaling; // Will be more specific if needed
using IMSystem.Protocol.DTOs.Notifications.Common;
using IMSystem.Protocol.DTOs.Requests.User;
// using IMSystem.Protocol.DTOs.Notifications.Messages; // Covered by general Notifications or more specific ones
using IMSystem.Protocol.DTOs.Notifications.Signaling;

namespace IMSystem.Client.Core.Services
{
    public class SignalRService : ISignalRService
    {
        private readonly ILogger<SignalRService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessenger _messenger;

        private HubConnection _messagingHubConnection;
        private HubConnection _presenceHubConnection;
        private HubConnection _signalingHubConnection;

        private readonly string _messagingHubUrl;
        private readonly string _presenceHubUrl;
        private readonly string _signalingHubUrl;

        private Timer _heartbeatTimer;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30); // Example interval

        public SignalRService(
            ILogger<SignalRService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IMessenger messenger)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _messenger = messenger;

            _messagingHubUrl = _configuration["SignalRHubs:MessagingHub"];
            _presenceHubUrl = _configuration["SignalRHubs:PresenceHub"];
            _signalingHubUrl = _configuration["SignalRHubs:SignalingHub"];

            // Defer initialization to ConnectAsync to ensure auth service is ready
            // InitializeHubConnections();
        }

        private IAuthService GetAuthService() => _serviceProvider.GetRequiredService<IAuthService>();

        private void InitializeHubConnections()
        {
            _logger.LogInformation("Initializing Hub connections...");
            _messagingHubConnection = CreateHubConnection(_messagingHubUrl, "MessagingHub");
            RegisterMessagingHubHandlers(_messagingHubConnection);

            _presenceHubConnection = CreateHubConnection(_presenceHubUrl, "PresenceHub");
            RegisterPresenceHubHandlers(_presenceHubConnection);

            _signalingHubConnection = CreateHubConnection(_signalingHubUrl, "SignalingHub");
            RegisterSignalingHubHandlers(_signalingHubConnection);
            _logger.LogInformation("Hub connections initialized.");
        }

        private HubConnection CreateHubConnection(string hubUrl, string hubName)
        {
            if (string.IsNullOrEmpty(hubUrl))
            {
                _logger.LogError($"{hubName} URL is not configured.");
                throw new InvalidOperationException($"{hubName} URL is not configured. Please check your appsettings.json.");
            }

            var hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var authService = GetAuthService();
                        if (authService.IsAuthenticated())
                        {
                            return authService.Token;
                        }
                        // 如果未认证，可以返回 null 或空字符串，或者根据需要抛出异常
                        // SignalR 客户端库会处理 Token 为 null 的情况（通常是不发送 Authorization 头）
                        _logger.LogWarning($"Attempted to get token for {hubName} but user is not authenticated.");
                        return null; 
                    };
                })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning(error, $"Connection to {hubName} lost. Attempting to reconnect...");
                // Optionally, notify the UI about the reconnection attempt
                return Task.CompletedTask;
            };

            hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation($"Successfully reconnected to {hubName} with new Connection ID: {connectionId}.");
                // Optionally, notify the UI about successful reconnection
                if (hubName == "PresenceHub")
                {
                    StartHeartbeat();
                }
                return Task.CompletedTask;
            };

            hubConnection.Closed += async (error) =>
            {
                _logger.LogError(error, $"Connection to {hubName} closed.");
                if (hubName == "PresenceHub")
                {
                    StopHeartbeat();
                }
                // The WithAutomaticReconnect should handle reconnection attempts.
                // If manual intervention is needed or logging for permanent closure:
                if (error != null)
                {
                    _logger.LogError(error, $"Connection to {hubName} was closed due to an error. Automatic reconnect will attempt to recover.");
                }
                else
                {
                    _logger.LogInformation($"Connection to {hubName} was closed gracefully or automatic reconnect exhausted retries.");
                }
                await Task.CompletedTask; // Ensure the handler is async if further async work is done
            };
            
            return hubConnection;
        }

        public HubConnection GetMessagingHubConnection() => _messagingHubConnection;
        
        public async Task<Result> MarkMessageAsReadAsync(MarkMessagesAsReadRequest request)
        {
            var hubConnection = GetMessagingHubConnection();
            if (hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.InvokeAsync("MarkMessageAsRead", request);
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    return Result.Failure(new Error("SignalR.InvokeFailed", $"Failed to mark message as read: {ex.Message}"));
                }
            }
            return Result.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }
        
        public async Task<Result> InitiateKeyExchangeAsync(InitiateKeyExchangeRequest request)
        {
            var hubConnection = GetMessagingHubConnection();
            if (hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await hubConnection.InvokeAsync("InitiateKeyExchange", request);
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    return Result.Failure(new Error("SignalR.InvokeFailed", $"Failed to initiate key exchange: {ex.Message}"));
                }
            }
            return Result.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }
        
        public async Task<Result<MessageSentConfirmationDto>> SendEncryptedMessageAsync(SendEncryptedMessageRequest request)
        {
            var hubConnection = GetMessagingHubConnection();
            if (hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    var confirmation = await hubConnection.InvokeAsync<MessageSentConfirmationDto>("SendEncryptedMessage", request);
                    return Result<MessageSentConfirmationDto>.Success(confirmation);
                }
                catch (Exception ex)
                {
                    return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.InvokeFailed", $"Failed to send encrypted message: {ex.Message}"));
                }
            }
            return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }
        public HubConnection GetPresenceHubConnection() => _presenceHubConnection;
        public HubConnection GetSignalingHubConnection() => _signalingHubConnection;

        public async Task StartMessagingHubAsync() => await StartHubAsync(_messagingHubConnection, "MessagingHub");
        public async Task StartPresenceHubAsync()
        {
            await StartHubAsync(_presenceHubConnection, "PresenceHub");
            if (IsPresenceHubConnected) StartHeartbeat(); // Start heartbeat after successful connection
        }
        public async Task StartSignalingHubAsync() => await StartHubAsync(_signalingHubConnection, "SignalingHub");

        public async Task StopMessagingHubAsync() => await StopHubAsync(_messagingHubConnection, "MessagingHub");
        public async Task StopPresenceHubAsync()
        {
            await StopHubAsync(_presenceHubConnection, "PresenceHub");
            StopHeartbeat(); // Stop heartbeat when presence hub is stopped
        }
        public async Task StopSignalingHubAsync() => await StopHubAsync(_signalingHubConnection, "SignalingHub");
        
        public async Task ConnectAsync()
        {
            if (_messagingHubConnection == null || _presenceHubConnection == null || _signalingHubConnection == null)
            {
                InitializeHubConnections();
            }

            _logger.LogInformation("Starting all Hub connections...");
            var tasks = new List<Task>
            {
                StartHubAsync(_messagingHubConnection, "MessagingHub"),
                StartHubAsync(_presenceHubConnection, "PresenceHub"),
                StartHubAsync(_signalingHubConnection, "SignalingHub")
            };
            await Task.WhenAll(tasks);
            _logger.LogInformation("All Hub connections attempt completed.");
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Stopping all Hub connections...");
            var tasks = new List<Task>();
            if (_messagingHubConnection != null) tasks.Add(StopHubAsync(_messagingHubConnection, "MessagingHub"));
            if (_presenceHubConnection != null) tasks.Add(StopHubAsync(_presenceHubConnection, "PresenceHub"));
            if (_signalingHubConnection != null) tasks.Add(StopHubAsync(_signalingHubConnection, "SignalingHub"));
            
            await Task.WhenAll(tasks);
            StopHeartbeat(); // Ensure heartbeat is stopped on manual disconnect
            _logger.LogInformation("All Hub connections stopped.");
        }

        // Keep existing StartHubAsync and StopHubAsync as private helpers
        private async Task StartHubAsync(HubConnection hubConnection, string hubName)
        {
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await hubConnection.StartAsync();
                    _logger.LogInformation($"Successfully connected to {hubName}. Connection ID: {hubConnection.ConnectionId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error starting connection to {hubName}.");
                    // 可以考虑抛出异常或进行其他错误处理
                }
            }
            else
            {
                _logger.LogInformation($"{hubName} is already in state: {hubConnection.State}.");
            }
        }

        private async Task StopHubAsync(HubConnection hubConnection, string hubName)
        {
            if (hubConnection.State == HubConnectionState.Connected || hubConnection.State == HubConnectionState.Connecting)
            {
                try
                {
                    await hubConnection.StopAsync();
                    _logger.LogInformation($"Successfully disconnected from {hubName}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error stopping connection to {hubName}.");
                }
            }
            else
            {
                _logger.LogInformation($"{hubName} is already in state: {hubConnection.State}. No action taken.");
            }
        }

        public bool IsMessagingHubConnected => _messagingHubConnection?.State == HubConnectionState.Connected;
        public bool IsPresenceHubConnected => _presenceHubConnection?.State == HubConnectionState.Connected;
        public bool IsSignalingHubConnected => _signalingHubConnection?.State == HubConnectionState.Connected;

        #region Hub Event Handlers Registration

        private void RegisterMessagingHubHandlers(HubConnection connection)
        {
            _logger.LogInformation("Registering handlers for MessagingHub...");
            // Messages
            connection.On<MessageDto>(SignalRClientMethods.ReceiveMessage, (message) =>
            {
                _logger.LogInformation("Received message: {MessageId} from {SenderId}", message.MessageId, message.SenderId);
                _messenger.Send(message);
            });
            connection.On<MessageSentConfirmationDto>(SignalRClientMethods.MessageSentConfirmation, (confirmation) =>
            {
                _logger.LogInformation("Message sent confirmation: ClientMessageId: {ClientMessageId}, ServerMessageId: {ServerMessageId}, Status: {Status}", confirmation.ClientMessageId, confirmation.ServerMessageId, confirmation.Status);
                _messenger.Send(confirmation);
            });
            connection.On<MessageRecalledNotificationDto>(SignalRClientMethods.MessageRecalled, (notification) =>
            {
                _logger.LogInformation("Message recalled: {MessageId}", notification.MessageId);
                _messenger.Send(notification);
            });
            connection.On<MessageReadNotificationDto>(SignalRClientMethods.ReceiveMessageReadNotification, (notification) =>
            {
                _logger.LogInformation("Message read notification: {MessageId} by {ReaderUserId}", notification.MessageId, notification.ReaderUserId);
                _messenger.Send(notification);
            });
            connection.On<MessageEditedNotificationDto>(SignalRClientMethods.ReceiveMessageEditedNotification, (notification) =>
            {
                _logger.LogInformation("Message edited notification: {MessageId}", notification.MessageId);
                _messenger.Send(notification);
            });

            // Friend Notifications (assuming handled by MessagingHub as per task description context)
            connection.On<NewFriendRequestNotificationDto>(SignalRClientMethods.NewFriendRequest, (notification) =>
            {
                _logger.LogInformation("New friend request from: {RequesterId}", notification.RequesterId);
                _messenger.Send(notification);
            });
            connection.On<FriendRequestAcceptedNotificationDto>(SignalRClientMethods.FriendRequestAccepted, (notification) =>
            {
                _logger.LogInformation("Friend request accepted by: {AcceptorId}", notification.AcceptorId);
                _messenger.Send(notification);
            });
            connection.On<FriendRequestRejectedNotificationDto>(SignalRClientMethods.ReceiveFriendRequestRejected, (notification) =>
            {
                _logger.LogInformation("Friend request rejected by: {RejecterId}", notification.RejecterId);
                _messenger.Send(notification);
            });
            connection.On<FriendRemovedNotificationDto>(SignalRClientMethods.ReceiveFriendRemoved, (notification) =>
            {
                _logger.LogInformation("Friend removed: {RemovedFriendUserId} by {RemoverUserId}", notification.RemovedFriendUserId, notification.RemoverUserId);
                _messenger.Send(notification);
            });
            connection.On<FriendGroupsReorderedNotificationDto>(SignalRClientMethods.FriendGroupsReordered, (notification) =>
            {
                _logger.LogInformation("Friend groups reordered for user: {UserId}", notification.UserId);
                _messenger.Send(notification);
            });

            connection.On<FriendGroupUpdatedNotificationDto>(SignalRClientMethods.FriendGroupUpdated, (notification) =>
            {
                _logger.LogInformation("Friend group updated: {GroupId}", notification.GroupId);
                _messenger.Send(notification);
            });


            // Group Notifications (assuming handled by MessagingHub)
            connection.On<GroupCreatedNotificationDto>(SignalRClientMethods.GroupCreated, (notification) =>
            {
                _logger.LogInformation("Group created: {GroupId}", notification.GroupId);
                _messenger.Send(notification);
            });
            connection.On<GroupDeletedNotificationDto>(SignalRClientMethods.GroupDeleted, (notification) =>
            {
                _logger.LogInformation("Group deleted: {GroupId}", notification.GroupId);
                _messenger.Send(notification);
            });
            connection.On<GroupDetailsUpdatedNotificationDto>(SignalRClientMethods.GroupDetailsUpdated, (notification) =>
            {
                _logger.LogInformation("Group details updated: {GroupId}", notification.GroupId);
                _messenger.Send(notification);
            });
            connection.On<NewGroupInvitationNotificationDto>(SignalRClientMethods.NewGroupInvitationNotification, (notification) =>
            {
                _logger.LogInformation("New group invitation: Group {GroupId} from {InviterId}", notification.GroupId, notification.InviterId);
                _messenger.Send(notification);
            });
            connection.On<UserJoinedGroupNotificationDto>(SignalRClientMethods.UserJoinedGroup, (notification) => // Or GroupMemberJoined
            {
                _logger.LogInformation("User {UserId} joined group {GroupId}", notification.UserId, notification.GroupId);
                _messenger.Send(notification);
            });
            connection.On<UserLeftGroupNotificationDto>(SignalRClientMethods.UserLeftGroup, (notification) =>
            {
                _logger.LogInformation("User {UserId} left group {GroupId}", notification.UserId, notification.GroupId);
                _messenger.Send(notification);
            });
            connection.On<GroupMemberKickedNotificationDto>(SignalRClientMethods.GroupMemberKicked, (notification) =>
            {
                _logger.LogInformation("User {KickedUserId} kicked from group {GroupId} by {ActorUserId}", notification.KickedUserId, notification.GroupId, notification.ActorUserId);
                _messenger.Send(notification);
            });
            connection.On<GroupOwnershipTransferredNotificationDto>(SignalRClientMethods.GroupOwnershipTransferred, (notification) =>
            {
                _logger.LogInformation("Group {GroupId} ownership transferred from {OldOwnerId} to {NewOwnerId} by {ActorUserId}",
                    notification.GroupId,
                    notification.OldOwner?.UserId,
                    notification.NewOwner?.UserId,
                    notification.ActorUserId);
                _messenger.Send(notification);
            });
            connection.On<GroupMemberRoleUpdatedNotificationDto>(SignalRClientMethods.GroupMemberRoleUpdated, (notification) =>
            {
                _logger.LogInformation("User {TargetMemberUserId} role in group {GroupId} updated from {OldRole} to {NewRole} by {ActorUserId}",
                    notification.TargetMemberUserId,
                    notification.GroupId,
                    notification.OldRole,
                    notification.NewRole,
                    notification.ActorUserId);
                _messenger.Send(notification);
            });
            connection.On<GroupAnnouncementSetNotificationDto>(SignalRClientMethods.GroupAnnouncementUpdated, (notification) => // Mapped to GroupAnnouncementUpdated from SignalRClientMethods
            {
                _logger.LogInformation("Group {GroupId} announcement updated by {ActorUserId}. Announcement: {Announcement}", notification.GroupId, notification.ActorUserId, notification.Announcement);
                _messenger.Send(notification);
            });
            
            // Other general notifications
            connection.On<SignalRErrorDto>(SignalRClientMethods.ReceiveError, (error) =>
            {
                _logger.LogError("Received error from SignalR Hub: {Code} - {Message}", error.Code, error.Message);
                _messenger.Send(error);
            });
        }

        private void RegisterPresenceHubHandlers(HubConnection connection)
        {
            _logger.LogInformation("Registering handlers for PresenceHub...");
            connection.On<UserPresenceChangedNotificationDto>(SignalRClientMethods.UserPresenceChanged, (notification) =>
            {
                _logger.LogInformation("User presence changed: {UserId} IsOnline: {IsOnline}, CustomStatus: {CustomStatus}", notification.UserId, notification.IsOnline, notification.CustomStatus);
                _messenger.Send(notification);
            });
            connection.On<UserTypingBroadcastDto>(SignalRClientMethods.ReceiveTypingNotification, (notification) =>
            {
                _logger.LogInformation("User {UserId} is typing ({IsTyping}) in chat {ChatType}:{ChatId}", notification.UserId, notification.IsTyping, notification.ChatType, notification.ChatId);
                _messenger.Send(notification);
            });
            // Heartbeat is started in StartPresenceHubAsync and on Reconnected event for PresenceHub
        }

        private void RegisterSignalingHubHandlers(HubConnection connection)
        {
            _logger.LogInformation("Registering handlers for SignalingHub...");

            // 使用 CallStateChangedNotificationDto 处理 CallInvitedEvent
            connection.On<CallStateChangedNotificationDto>(SignalRClientMethods.CallInvited, (notification) =>
            {
                _logger.LogInformation("Call invited (using CallStateChangedNotificationDto): CallId {CallId}, State {CallState}, Caller {CallerId}, Callee {CalleeId}",
                                     notification.CallId, notification.CallState, notification.CallerId, notification.CalleeId);
                _messenger.Send(notification);
                // 注意: CallInvitedEvent 使用 CallStateChangedNotificationDto。
                // 如果需要更具体的 CallInvitedNotificationDto，请创建并替换。
            });

            // 使用 CallStateChangedNotificationDto 处理 CallAnsweredEvent
            connection.On<CallStateChangedNotificationDto>(SignalRClientMethods.CallAnswered, (notification) =>
            {
                _logger.LogInformation("Call answered (using CallStateChangedNotificationDto): CallId {CallId}, State {CallState}, Responder {CalleeId}",
                                     notification.CallId, notification.CallState, notification.CalleeId); // 通常应答者是被叫方
                _messenger.Send(notification);
                // 注意: CallAnsweredEvent 使用 CallStateChangedNotificationDto。
                // 如果需要更具体的 CallAnsweredNotificationDto，请创建并替换。
            });

            // 使用 CallStateChangedNotificationDto 处理 CallRejectedEvent
            connection.On<CallStateChangedNotificationDto>(SignalRClientMethods.CallRejected, (notification) =>
            {
                _logger.LogInformation("Call rejected (using CallStateChangedNotificationDto): CallId {CallId}, State {CallState}, Responder {CalleeId}, Reason {Reason}",
                                     notification.CallId, notification.CallState, notification.CalleeId, notification.Reason); // 通常拒绝者是被叫方
                _messenger.Send(notification);
                // 注意: CallRejectedEvent 使用 CallStateChangedNotificationDto。
                // 如果需要更具体的 CallRejectedNotificationDto，请创建并替换。
            });

            // 使用 CallStateChangedNotificationDto 处理 CallHungupEvent
            connection.On<CallStateChangedNotificationDto>(SignalRClientMethods.CallHungup, (notification) =>
            {
                // 对于挂断事件，需要确定是主叫还是被叫挂断，日志中可以同时记录两者
                _logger.LogInformation("Call hungup (using CallStateChangedNotificationDto): CallId {CallId}, State {CallState}, Caller {CallerId}, Callee {CalleeId}, Reason {Reason}",
                                     notification.CallId, notification.CallState, notification.CallerId, notification.CalleeId, notification.Reason);
                _messenger.Send(notification);
                // 注意: CallHungupEvent 使用 CallStateChangedNotificationDto。
                // 如果需要更具体的 CallHungupNotificationDto，请创建并替换。
            });
            
            connection.On<SdpExchangeNotificationDto>(SignalRClientMethods.SdpExchanged, (notification) =>
            {
                _logger.LogInformation("SDP exchanged for CallId {CallId} from {SenderId}", notification.CallId, notification.SenderId);
                _messenger.Send(notification);
            });
            connection.On<IceCandidateNotificationDto>(SignalRClientMethods.IceCandidateExchanged, (notification) =>
            {
                _logger.LogInformation("ICE candidate exchanged for CallId {CallId} from {SenderId}", notification.CallId, notification.SenderId);
                _messenger.Send(notification);
            });
            connection.On<CallStateChangedNotificationDto>(SignalRClientMethods.CallStateChanged, (notification) =>
            {
                _logger.LogInformation("Call state changed for CallId {CallId}: CallState {CallState}, Reason: {Reason}", notification.CallId, notification.CallState, notification.Reason);
                _messenger.Send(notification);
            });
        }

        #endregion

        #region Heartbeat
        private void StartHeartbeat()
        {
            if (_presenceHubConnection?.State == HubConnectionState.Connected)
            {
                _logger.LogInformation("Starting PresenceHub heartbeat.");
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = new Timer(SendHeartbeatAsync, null, TimeSpan.Zero, _heartbeatInterval);
            }
            else
            {
                _logger.LogWarning("Cannot start heartbeat, PresenceHub is not connected.");
            }
        }

        private void StopHeartbeat()
        {
            _logger.LogInformation("Stopping PresenceHub heartbeat.");
            _heartbeatTimer?.Change(Timeout.Infinite, 0); // Stop the timer
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }

        private async void SendHeartbeatAsync(object state)
        {
            if (_presenceHubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    var authService = GetAuthService();
                    var currentUserId = authService.CurrentUserId; // This is Guid?
                    if (authService.IsAuthenticated() && currentUserId.HasValue)
                    {
                        var heartbeatDto = new HeartbeatRequestDto { UserId = currentUserId.Value.ToString(), Timestamp = DateTime.UtcNow };
                        await _presenceHubConnection.InvokeAsync("Heartbeat", heartbeatDto);
                        _logger.LogDebug("Heartbeat sent to PresenceHub for user {UserId}.", heartbeatDto.UserId);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot send heartbeat: User not authenticated or UserId is missing.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending heartbeat to PresenceHub.");
                    if (_presenceHubConnection.State != HubConnectionState.Connected)
                    {
                        StopHeartbeat();
                    }
                }
            }
            else
            {
                _logger.LogWarning("PresenceHub not connected. Stopping heartbeat.");
                StopHeartbeat();
            }
        }
        #endregion
    }
}