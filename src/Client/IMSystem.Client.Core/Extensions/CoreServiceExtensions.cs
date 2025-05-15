// src/Client/IMSystem.Client.Core/Extensions/CoreServiceExtensions.cs
using IMSystem.Client.Core.Interfaces;
using IMSystem.Client.Core.Services;
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection.Abstractions
using CommunityToolkit.Mvvm.Messaging;

namespace IMSystem.Client.Core.Extensions
{
    public static class CoreServiceExtensions
    {
        public static IServiceCollection AddCoreClientServices(this IServiceCollection services)
        {
            services.AddSingleton<IApiService, ApiService>();
            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<ISignalRService, SignalRService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IFriendsService, FriendsService>();
            services.AddSingleton<IFriendGroupsService, FriendGroupsService>();
            services.AddSingleton<IGroupService, GroupService>();
            services.AddSingleton<IChatService, ChatService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ISignalingService, SignalingService>();
            services.AddSingleton<IWebRTCService, WebRTCService>();
            services.AddSingleton<IMessenger, WeakReferenceMessenger>();
            services.AddHttpClient();
            
            return services;
        }
    }
}