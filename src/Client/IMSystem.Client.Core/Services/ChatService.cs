using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Messages;
using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Protocol.DTOs.Requests.Messages;
using IMSystem.Protocol.Enums;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging; // Added for ILogger
using IMSystem.Protocol.DTOs.Responses.Messages;
using System;
using System.Collections.Generic; // Added for List
using System.Threading.Tasks;
// Assuming IApiService and ISignalRService are in this namespace or a referenced one
// If they are in IMSystem.Client.Core.Interfaces.Services, the existing using is sufficient.
// If they are in a different sub-namespace of IMSystem.Client.Core.Interfaces, add specific using.
// For now, I'll assume they are correctly referenced or in the same namespace as IChatService.
// using IMSystem.Client.Core.Models; // For Error if not in Common
// using IMSystem.Shared.Exceptions; // For ApiException if it's defined here

namespace IMSystem.Client.Core.Services
{
    public class ChatService : IChatService
    {
        private readonly IApiService _apiService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<ChatService> _logger; // Added logger

        public ChatService(IApiService apiService, ISignalRService signalRService, ILogger<ChatService> logger) // Added logger
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Added logger
        }

        public async Task<Result<PagedResult<MessageDto>>> GetUserMessagesAsync(string otherUserId, int pageNumber, int pageSize, long? beforeSequence = null, int? limit = null)
        {
            var endpoint = $"/api/Messages/user/{otherUserId}?pageNumber={pageNumber}&pageSize={pageSize}";
            if (beforeSequence.HasValue)
            {
                endpoint += $"&beforeSequence={beforeSequence.Value}";
            }
            if (limit.HasValue)
            {
                endpoint += $"&limit={limit.Value}";
            }
            // Assuming GetAsync returns PagedResult<T> directly and we need to wrap it in a Result
            var pagedResult = await _apiService.GetAsync<PagedResult<MessageDto>>(endpoint);
            return Result<PagedResult<MessageDto>>.Success(pagedResult); // Or handle potential errors from GetAsync if it can fail
        }

        public async Task<Result<PagedResult<MessageDto>>> GetGroupMessagesAsync(string groupId, int pageNumber, int pageSize, long? beforeSequence = null, int? limit = null)
        {
            var endpoint = $"/api/Messages/group/{groupId}?pageNumber={pageNumber}&pageSize={pageSize}";
            if (beforeSequence.HasValue)
            {
                endpoint += $"&beforeSequence={beforeSequence.Value}";
            }
            if (limit.HasValue)
            {
                endpoint += $"&limit={limit.Value}";
            }
            // Assuming GetAsync returns PagedResult<T> directly and we need to wrap it in a Result
            var pagedResult = await _apiService.GetAsync<PagedResult<MessageDto>>(endpoint);
            return Result<PagedResult<MessageDto>>.Success(pagedResult); // Or handle potential errors
        }

        public async Task<Result<MessageSentConfirmationDto>> SendUserMessageAsync(SendMessageDto message)
        {
            var hubConnection = _signalRService.GetMessagingHubConnection();
            if (hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected) // Fully qualify HubConnectionState
            {
                try
                {
                    var confirmation = await hubConnection.InvokeAsync<MessageSentConfirmationDto>("SendUserMessage", message);
                    return Result<MessageSentConfirmationDto>.Success(confirmation);
                }
                catch (Exception ex)
                {
                    // Log error ex
                    return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.InvokeFailed", $"Failed to send user message: {ex.Message}"));
                }
            }
            return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }

        public async Task<Result<MessageSentConfirmationDto>> SendGroupMessageAsync(SendMessageDto message)
        {
            var hubConnection = _signalRService.GetMessagingHubConnection();
            if (hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected) // Fully qualify HubConnectionState
            {
                try
                {
                    var confirmation = await hubConnection.InvokeAsync<MessageSentConfirmationDto>("SendGroupMessage", message);
                    return Result<MessageSentConfirmationDto>.Success(confirmation);
                }
                catch (Exception ex)
                {
                    // Log error ex
                    return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.InvokeFailed", $"Failed to send group message: {ex.Message}"));
                }
            }
            return Result<MessageSentConfirmationDto>.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }

        public async Task<Result> MarkMessageAsReadAsync(string messageId, string chatPartnerId, ProtocolChatType chatType)
        {
            var hubConnection = _signalRService.GetMessagingHubConnection();
            if (hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected) // Fully qualify HubConnectionState
            {
                try
                {
                    // MarkMessageAsRead might not return a value or a specific DTO.
                    // If it returns a specific DTO for confirmation, adjust InvokeAsync<T> accordingly.
                    await hubConnection.InvokeAsync("MarkMessageAsRead", messageId, chatPartnerId, chatType);
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    // Log error ex
                    return Result.Failure(new Error("SignalR.InvokeFailed", $"Failed to mark message as read: {ex.Message}"));
                }
            }
            return Result.Failure(new Error("SignalR.NotConnected", "MessagingHub is not connected."));
        }

        public async Task<Result> MarkMessagesAsReadAsync(MarkMessagesAsReadRequest request)
        {
            // Assuming PostAsync returns Task and we need to wrap it in a Result
            await _apiService.PostAsync("/api/Messages/read", request);
            return Result.Success(); // Or handle potential errors
        }

        public async Task<Result> RecallMessageAsync(string messageId)
        {
            // Assuming PostAsync for recall might take an empty body or a specific request object.
            // For now, assuming it might need an empty object or the API service handles it.
            // If PostAsync(url) is not available, this needs to be PostAsync(url, object)
            await _apiService.PostAsync($"/api/Messages/{messageId}/recall", new { }); // Sending an empty object
            return Result.Success(); // Or handle potential errors
        }

        public async Task<Result> EditMessageAsync(string messageId, EditMessageRequest request)
        {
            // Assuming PutAsync returns Task and we need to wrap it in a Result
            await _apiService.PutAsync($"/api/Messages/{messageId}", request);
            return Result.Success(); // Or handle potential errors
        }

        public async Task SendTypingNotificationAsync(string recipientId, ProtocolChatType chatType)
        {
            var hubConnection = _signalRService.GetPresenceHubConnection();
            if (hubConnection?.State == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected) // Fully qualify HubConnectionState
            {
                try
                {
                    // SendAsync is typically used for methods that don't expect a response.
                    await hubConnection.SendAsync("SendTypingNotification", recipientId, chatType);
                }
                catch (Exception ex)
                {
                    // Log error ex
                    // Consider how to handle errors for fire-and-forget notifications.
                    // For now, just logging or ignoring.
                    // A more robust solution might involve a logger service.
                    System.Diagnostics.Debug.WriteLine($"Error sending typing notification: {ex.Message}");
                }
            }
            else
            {
                 System.Diagnostics.Debug.WriteLine("PresenceHub is not connected. Cannot send typing notification.");
            }
       }

       public async Task<Result<List<MessageDto>>> GetMessagesAfterSequenceAsync(string chatPartnerId, ProtocolChatType chatType, long afterSequence, int? limit = null)
       {
           if (string.IsNullOrWhiteSpace(chatPartnerId))
           {
               return Result<List<MessageDto>>.Failure(new Error("Validation.ChatPartnerIdRequired", "Chat partner ID cannot be empty."));
           }

           try
           {
               // Ensure relevant using statements are present at the top of the file:
               // using IMSystem.Protocol.DTOs.Requests.Messages; // For GetMessagesAfterSequenceRequest
               // using IMSystem.Protocol.Enums; // For ProtocolChatType (already there)
               // using IMSystem.Protocol.Common; // For Result, Error (already there)
               // using IMSystem.Protocol.DTOs.Messages; // For MessageDto (already there)
               // using Microsoft.Extensions.Logging; // For ILogger (added)
               // using System.Collections.Generic; // For List (added)
               // using IMSystem.Client.Core.Exceptions; // Or wherever ApiException is defined

               var requestDto = new GetMessagesAfterSequenceRequest
               {
                   RecipientId = Guid.Parse(chatPartnerId), // Assuming chatPartnerId is a Guid string
                   ChatType = chatType,
                   AfterSequence = afterSequence,
                   Limit = limit
               };

               // Server documentation indicates /api/Messages/after-sequence uses POST
               var messages = await _apiService.PostAsync<GetMessagesAfterSequenceRequest, List<MessageDto>>("/api/Messages/after-sequence", requestDto);
               
               if (messages != null)
               {
                   return Result<List<MessageDto>>.Success(messages);
               }
               // It's possible PostAsync returns null if the API returns 204 No Content or similar,
               // and the IApiService implementation deserializes this to null.
               // Or, if PostAsync itself throws an exception for non-success status codes that aren't ApiException.
               _logger.LogWarning("No messages found or API returned no content for chatPartnerId {ChatPartnerId}, afterSequence {AfterSequence}.", chatPartnerId, afterSequence);
               return Result<List<MessageDto>>.Failure(new Error("ChatService.GetMessages.NoContent", "No messages found or API returned no content."));
           }
           // Assuming ApiException is a custom exception type that your IApiService might throw.
           // Ensure it has an 'Error' property of type IMSystem.Protocol.Common.Error.
           // If ApiException is from a library like Refit, the error handling might be different.
           // For example, ex.GetContentAsAsync<ApiErrorResponse>() or similar.
           // For now, proceeding with the assumption that 'apiEx.Error' is of the correct 'Error' type.
           catch (ApiException apiEx) // Replace ApiException with the actual exception type if different
           {
               // Assuming apiEx.Error is of type ApiErrorResponse
               // Use apiEx.Error.Title or apiEx.Error.Detail for the message
               string apiErrorMessage = apiEx.Error?.Title ?? apiEx.Error?.Detail ?? "N/A";
               string apiErrorCode = apiEx.Error?.ErrorCode ?? "ChatService.ApiError.Unknown";

               _logger.LogError(apiEx, "API error fetching messages after sequence {AfterSequence} for chat {ChatPartnerId} ({ChatType}). Error Code: {ApiErrorCode}, Error: {ApiErrorMessage}",
                   afterSequence, chatPartnerId, chatType, apiErrorCode, apiErrorMessage);
               
               // Create a new Error object from ApiErrorResponse properties
               var error = new Error(apiErrorCode, apiErrorMessage);
               if (apiEx.Error?.Errors != null && apiEx.Error.Errors.Any())
               {
                   // Optionally, you can serialize the validation errors into the message or a structured detail
                   // For simplicity, just appending a note if validation errors exist.
                   error = new Error(apiErrorCode, $"{apiErrorMessage} Validation errors: {string.Join("; ", apiEx.Error.Errors.Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}"))}");
               }
               return Result<List<MessageDto>>.Failure(error);
           }
           catch (FormatException formatEx)
           {
               _logger.LogError(formatEx, "Invalid chatPartnerId format: {ChatPartnerId}. Could not parse to Guid.", chatPartnerId);
               return Result<List<MessageDto>>.Failure(new Error("Validation.InvalidChatPartnerId", "Chat partner ID is not a valid GUID."));
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Unexpected error fetching messages after sequence {AfterSequence} for chat {ChatPartnerId} ({ChatType}).",
                   afterSequence, chatPartnerId, chatType);
               return Result<List<MessageDto>>.Failure(new Error("ChatService.GetMessages.UnexpectedError", "An unexpected error occurred."));
           }
       }
       /// <summary>
       /// Gets the list of users who have read a specific group message.
       /// </summary>
       /// <param name="messageId">The ID of the message.</param>
       /// <returns>A result containing the list of users who have read the message.</returns>
       public async Task<Result<GetGroupMessageReadUsersResponse>> GetGroupMessageReadByAsync(string messageId)
       {
           return await HandleApiResponseAsync(() => _apiService.GetAsync<GetGroupMessageReadUsersResponse>($"api/Messages/group/{messageId}/readby"));
       }

       /// <summary>
       /// Sends an encrypted message via API.
       /// </summary>
       /// <param name="request">The request containing the encrypted message details.</param>
       /// <returns>A result indicating success or failure.</returns>
       public async Task<Result> SendEncryptedMessageAsync(SendEncryptedMessageRequest request)
       {
           return await HandleApiResponseAsync(() => _apiService.PostAsync("api/Messages/encrypted", request));
       }

       /// <summary>
       /// Handles API responses and standardizes the result.
       /// </summary>
       /// <typeparam name="T">The type of the result.</typeparam>
       /// <param name="apiCall">The API call to execute.</param>
       /// <returns>A standardized result containing the API response or an error.</returns>
       private async Task<Result<T>> HandleApiResponseAsync<T>(Func<Task<T>> apiCall)
       {
           try
           {
               var response = await apiCall();
               return Result<T>.Success(response);
           }
           catch (ApiException apiEx)
           {
               var error = new Error(apiEx.Error?.ErrorCode ?? "ApiError", apiEx.Error?.Detail ?? "An error occurred.");
               return Result<T>.Failure(error);
           }
           catch (Exception ex)
           {
               return Result<T>.Failure(new Error("UnexpectedError", ex.Message));
           }
       }

       /// <summary>
       /// Handles API responses for void methods and standardizes the result.
       /// </summary>
       /// <param name="apiCall">The API call to execute.</param>
       /// <returns>A standardized result indicating success or failure.</returns>
       private async Task<Result> HandleApiResponseAsync(Func<Task> apiCall)
       {
           try
           {
               await apiCall();
               return Result.Success();
           }
           catch (ApiException apiEx)
           {
               var error = new Error(apiEx.Error?.ErrorCode ?? "ApiError", apiEx.Error?.Detail ?? "An error occurred.");
               return Result.Failure(error);
           }
           catch (Exception ex)
           {
               return Result.Failure(new Error("UnexpectedError", ex.Message));
           }
       }
       /// <summary>
       /// 通过 HTTP API 发送用户消息。
       /// </summary>
       /// <param name="message">要发送的消息数据。</param>
       /// <returns>一个包含结果的对象，如果成功，则包含服务端生成的消息ID。</returns>
       public async Task<Result<Guid>> SendUserMessageHttpAsync(SendMessageDto message)
       {
           try
           {
               var response = await _apiService.PostAsync<SendMessageDto, Guid>("/api/Messages/user", message);
               return Result<Guid>.Success(response);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to send user message via HTTP.");
               return Result<Guid>.Failure(new Error("HttpError", ex.Message));
           }
       }

       /// <summary>
       /// 通过 HTTP API 发送群组消息。
       /// </summary>
       /// <param name="message">要发送的消息数据。</param>
       /// <returns>一个包含结果的对象，如果成功，则包含服务端生成的消息ID。</returns>
       public async Task<Result<Guid>> SendGroupMessageHttpAsync(SendMessageDto message)
       {
           try
           {
               var response = await _apiService.PostAsync<SendMessageDto, Guid>("/api/Messages/group", message);
               return Result<Guid>.Success(response);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to send group message via HTTP.");
               return Result<Guid>.Failure(new Error("HttpError", ex.Message));
           }
       }

       /// <summary>
       /// 通过 SignalR Hub 发起密钥交换。
       /// </summary>
       /// <param name="request">密钥交换的请求详情。</param>
       /// <returns>一个表示操作结果的对象。</returns>
       public async Task<Result> InitiateKeyExchangeAsync(InitiateKeyExchangeRequest request)
       {
           var hubConnection = _signalRService.GetMessagingHubConnection();
           if (hubConnection?.State == HubConnectionState.Connected)
           {
               try
               {
                   await hubConnection.InvokeAsync("InitiateKeyExchange", request);
                   return Result.Success();
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, "Failed to initiate key exchange via SignalR.");
                   return Result.Failure(new Error("SignalRError", ex.Message));
               }
           }
           return Result.Failure(new Error("SignalRNotConnected", "MessagingHub is not connected."));
       }
   }
}