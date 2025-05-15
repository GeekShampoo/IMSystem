using IMSystem.Server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Persistence
{
    /// <summary>
    /// 定义用户仓储的接口，用于执行与 <see cref="User"/> 实体相关的数据库操作。
    /// </summary>
    public interface IUserRepository : IGenericRepository<User>
    {

        /// <summary>
        /// 根据用户名异步获取用户。
        /// </summary>
        /// <param name="username">用户名。</param>
        /// <returns>表示异步操作的结果，包含找到的 <see cref="User"/> 实体；如果未找到，则返回 null。</returns>
        Task<User?> GetByUsernameAsync(string username);

        /// <summary>
        /// 根据电子邮件地址异步获取用户。
        /// </summary>
        /// <param name="email">电子邮件地址。</param>
        /// <returns>表示异步操作的结果，包含找到的 <see cref="User"/> 实体；如果未找到，则返回 null。</returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// 异步检查是否存在具有指定ID的用户。
        /// <summary>
        /// 异步检查是否存在具有指定ID的用户。
        /// </summary>
        /// <param name="id">用户的唯一标识符。</param>
        /// <returns>如果存在具有指定ID的用户，则为 true；否则为 false。</returns>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 异步检查是否存在具有指定用户名的用户。
        /// </summary>
        /// <param name="username">用户名。</param>
        /// <returns>如果存在具有指定用户名的用户，则为 true；否则为 false。</returns>
        Task<bool> ExistsByUsernameAsync(string username);

        /// <summary>
        /// 异步检查是否存在具有指定电子邮件地址的用户。
        /// </summary>
        /// <param name="email">电子邮件地址。</param>
        /// <returns>如果存在具有指定电子邮件地址的用户，则为 true；否则为 false。</returns>
        Task<bool> ExistsByEmailAsync(string email);

        /// <summary>
        /// 根据外部ID列表异步获取用户集合。
        /// </summary>
        /// <param name="externalIds">用户外部ID (Guid) 的集合。</param>
        /// <returns>表示异步操作的结果，包含找到的 <see cref="User"/> 实体集合。</returns>
        Task<IEnumerable<User>> GetUsersByExternalIdsAsync(IEnumerable<Guid> externalIds);

        /// <summary>
        /// Asynchronously gets a user by ID, including their profile information.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        /// <returns>The user entity with profile included, or null if not found.</returns>
        Task<User?> GetByIdWithProfileAsync(Guid id);


        /// <summary>
        /// 根据外部ID列表异步获取用户集合，并包含其个人资料信息。
        /// </summary>
        /// <param name="externalIds">用户外部ID (Guid) 的集合。</param>
        /// <returns>表示异步操作的结果，包含找到的 <see cref="User"/> 实体集合及其个人资料。</returns>
        Task<IEnumerable<User>> GetUsersByExternalIdsWithProfileAsync(IEnumerable<Guid> externalIds);

        /// <summary>
        /// Asynchronously finds a user by their email verification token.
        /// </summary>
        /// <param name="token">The email verification token.</param>
        /// <returns>The user entity if found and token is valid; otherwise, null.</returns>
        Task<User?> FindByEmailVerificationTokenAsync(string token);

        /// <summary>
        /// Asynchronously gets a user by username, including their profile information.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <returns>The user entity with profile included, or null if not found.</returns>
        Task<User?> GetByUsernameWithProfileAsync(string username);


        /// <summary>
        /// Asynchronously gets a collection of users by their IDs.
        /// </summary>
        /// <param name="userIds">A collection of user IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of User entities.</returns>
        Task<IEnumerable<User>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    }
}