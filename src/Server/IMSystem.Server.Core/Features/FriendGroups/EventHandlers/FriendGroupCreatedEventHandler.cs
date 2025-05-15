using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.FriendGroups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.EventHandlers;

public class FriendGroupCreatedEventHandler : INotificationHandler<FriendGroupCreatedEvent>
{
    private readonly ILogger<FriendGroupCreatedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;

    public FriendGroupCreatedEventHandler(
        ILogger<FriendGroupCreatedEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(FriendGroupCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling FriendGroupCreatedEvent for GroupId: {GroupId}, UserId: {UserId}",
            notification.GroupId, notification.UserId);

        // Notify the user who created the group that their UI should be updated.
        // The payload should contain enough information for the client to add the new group.
        var payload = new
        {
            notification.GroupId,
            notification.Name,
            notification.Order,
            notification.IsDefault,
            // Include other relevant details from FriendGroupDto if needed by client
        };

        // Method name on the client to handle this notification
        string clientMethodName = "FriendGroupCreated"; 

        try
        {
            await _chatNotificationService.SendNotificationAsync(
                notification.UserId.ToString(),
                clientMethodName,
                payload,
                cancellationToken);
            
            _logger.LogInformation("Successfully sent FriendGroupCreated notification to UserId: {UserId} for GroupId: {GroupId}",
                notification.UserId, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending FriendGroupCreated notification to UserId: {UserId} for GroupId: {GroupId}",
                notification.UserId, notification.GroupId);
            // Depending on the application's error handling strategy, this might throw or just log.
        }
    }
}