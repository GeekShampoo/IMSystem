using System.Collections.Generic;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services;

/// <summary>
/// Service responsible for sending notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user to notify.</param>
    /// <param name="messageType">A string identifying the type of notification (e.g., "GroupInvitationReceived", "NewGroupMember").</param>
    /// <param name="payload">The data/payload associated with the notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNotificationAsync(string userId, string messageType, object payload);

    /// <summary>
    /// Sends a notification to multiple users.
    /// </summary>
    /// <param name="userIds">A list of user IDs to notify.</param>
    /// <param name="messageType">A string identifying the type of notification.</param>
    /// <param name="payload">The data/payload associated with the notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNotificationToMultipleUsersAsync(IEnumerable<string> userIds, string messageType, object payload);
    
    /// <summary>
    /// Sends a notification to all users in a specific group/channel (e.g., via SignalR group).
    /// </summary>
    /// <param name="groupName">The name of the group/channel.</param>
    /// <param name="messageType">A string identifying the type of notification.</param>
    /// <param name="payload">The data/payload associated with the notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNotificationToGroupAsync(string groupName, string messageType, object payload);

    // Add more methods as needed, e.g., for system-wide notifications, email notifications, etc.
}