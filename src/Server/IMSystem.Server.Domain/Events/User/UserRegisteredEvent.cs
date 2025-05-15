using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.User
{
    public class UserRegisteredEvent : DomainEvent
    {
        public Guid UserId { get; }
        public string Username { get; }
        public string Email { get; }
        public DateTimeOffset RegisteredAt { get; }

        public UserRegisteredEvent(Guid userId, string username, string email, DateTimeOffset registeredAt)
            : base(entityId: userId, triggeredBy: userId) // 用户是自己注册的实体和触发者
        {
            UserId = userId;
            Username = username;
            Email = email;
            RegisteredAt = registeredAt;
        }
    }
}