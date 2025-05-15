using System;
using System.Collections.Generic;
using System.Linq; // For IQueryable
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Common; // For BaseEntity

namespace IMSystem.Server.Core.Interfaces.Persistence;

/// <summary>
/// 通用仓储接口，定义了对实体的基本CRUD操作。
/// </summary>
/// <typeparam name="TEntity">实体类型，必须继承自 BaseEntity。</typeparam>
public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// 根据ID异步获取实体。
    /// </summary>
    /// <param name="id">实体ID。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>如果找到，则返回实体；否则返回 null。</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步获取所有实体。
    /// </summary>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>实体列表。</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件异步查找实体。
    /// </summary>
    /// <param name="predicate">查询条件表达式。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>符合条件的实体列表。</returns>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步添加新实体。
    /// </summary>
    /// <param name="entity">要添加的实体。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步添加多个实体。
    /// </summary>
    /// <param name="entities">要添加的实体集合。</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体。
    /// </summary>
    /// <param name="entity">要更新的实体。</param>
    void Update(TEntity entity);
    
    /// <summary>
    /// 更新多个实体。
    /// </summary>
    /// <param name="entities">要更新的实体集合。</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 移除实体。
    /// </summary>
    /// <param name="entity">要移除的实体。</param>
    void Remove(TEntity entity);

    /// <summary>
    /// 移除多个实体。
    /// </summary>
    /// <param name="entities">要移除的实体集合。</param>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Returns an IQueryable for the entity set.
    /// <para>
    /// **Usage Note:** This method provides direct IQueryable access and should be used judiciously.
    /// Prefer defining specific, business-oriented query methods in derived repository interfaces
    /// (e.g., IUserRepository) that return concrete results (e.g., Task<List<TEntity>>, Task<TEntity?>).
    /// </para>
    /// <para>
    /// Direct use from application services is discouraged as it can lead to data access logic leakage
    /// and potential performance issues if not handled carefully. Consider this a low-level tool
    /// primarily for use within repository implementations.
    /// </para>
    /// </summary>
    /// <returns>An IQueryable for the entity set, allowing further LINQ composition before execution.</returns>
    IQueryable<TEntity> Queryable();

    // Optional: Add CountAsync and ExistsAsync if commonly needed
    /// <summary>
    /// Asynchronously counts the number of entities that satisfy a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>The number of elements that satisfy the condition in the input sequence.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously determines whether any entity satisfies a condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌。</param>
    /// <returns>true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}