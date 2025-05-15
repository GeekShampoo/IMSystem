using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Requests.Signaling;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMSystem.Client.Core.Services
{
    /// <summary>
    /// Implements <see cref="ISignalingService"/> to handle signaling interactions with the backend SignalingHub.
    /// </summary>
    public class SignalingService : ISignalingService
    {
        private readonly ISignalRService _signalRService;
        private readonly ILogger<SignalingService> _logger;
        private const string HubName = "SignalingHub";

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalingService"/> class.
        /// </summary>
        /// <param name="signalRService">The SignalR service for invoking Hub methods.</param>
        /// <param name="logger">The logger for logging messages.</param>
        public SignalingService(ISignalRService signalRService, ILogger<SignalingService> logger)
        {
            _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result> InviteUserAsync(CallInviteRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot invite user.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                return await hubConnection.InvokeAsync<Result>("InviteUser", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user to call. CalleeId: {CalleeId}, CallType: {CallType}", request.CalleeId, request.CallType);
                return Result.Failure(new Error("Signaling.InviteUser.Failed", $"Failed to invite user: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result> AnswerCallAsync(CallAnswerRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot answer call.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                return await hubConnection.InvokeAsync<Result>("AnswerCall", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error answering call. CallId: {CallId}", request.CallId);
                return Result.Failure(new Error("Signaling.AnswerCall.Failed", $"Failed to answer call: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result> RejectCallAsync(CallRejectRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot reject call.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                return await hubConnection.InvokeAsync<Result>("RejectCall", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call. CallId: {CallId}", request.CallId);
                return Result.Failure(new Error("Signaling.RejectCall.Failed", $"Failed to reject call: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result> HangupCallAsync(CallHangupRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot hang up call.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                return await hubConnection.InvokeAsync<Result>("HangupCall", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hanging up call. CallId: {CallId}", request.CallId);
                return Result.Failure(new Error("Signaling.HangupCall.Failed", $"Failed to hang up call: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result> SendSdpAsync(SdpExchangeRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot send SDP.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                // Note: The Hub method name for SDP exchange is "SendSdp" as per typical conventions.
                // Adjust if the actual Hub method name is different.
                return await hubConnection.InvokeAsync<Result>("SendSdp", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SDP. CallId: {CallId}, SdpType: {SdpType}", request.CallId, request.SdpType);
                return Result.Failure(new Error("Signaling.SendSdp.Failed", $"Failed to send SDP: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public async Task<Result> SendIceCandidateAsync(IceCandidateExchangeRequest request)
        {
            try
            {
                var hubConnection = _signalRService.GetSignalingHubConnection();
                if (hubConnection == null || hubConnection.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalingHub is not connected. Cannot send ICE candidate.");
                    return Result.Failure(new Error("Signaling.Hub.NotConnected", "SignalingHub is not connected."));
                }
                // Note: The Hub method name for ICE candidate exchange is "SendIceCandidate" as per typical conventions.
                // Adjust if the actual Hub method name is different.
                return await hubConnection.InvokeAsync<Result>("SendIceCandidate", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ICE candidate. CallId: {CallId}", request.CallId);
                return Result.Failure(new Error("Signaling.SendIceCandidate.Failed", $"Failed to send ICE candidate: {ex.Message}"));
            }
        }
    }
}