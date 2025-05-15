using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace IMSystem.Client.Core.Interfaces
{
    public interface ISignalRService
    {
        HubConnection GetMessagingHubConnection();
        HubConnection GetPresenceHubConnection();
        HubConnection GetSignalingHubConnection();

        Task ConnectAsync(); // Added
        Task DisconnectAsync(); // Added

        Task StartMessagingHubAsync();
        Task StartPresenceHubAsync();
        Task StartSignalingHubAsync();

        Task StopMessagingHubAsync();
        Task StopPresenceHubAsync();
        Task StopSignalingHubAsync();

        bool IsMessagingHubConnected { get; }
        bool IsPresenceHubConnected { get; }
        bool IsSignalingHubConnected { get; }
    }
}