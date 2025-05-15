using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using IMSystem.Client.Core.Interfaces;
using IMSystem.Protocol.DTOs.Responses.Groups;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IMSystem.Client.Core.Services
{
   public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;
        private readonly bool _autoMigrate;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration["DatabaseSettings:ConnectionString"] 
                ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "IMSystem.db")}";
            _logger = logger;
            _autoMigrate = configuration.GetValue<bool>("DatabaseSettings:AutoMigrate");
            // 初始化逻辑由外部显式调用EnsureDatabaseSchemaAsync方法
        }

        public async Task EnsureDatabaseSchemaAsync()
        {
            if (!_autoMigrate)
            {
                _logger.LogInformation("数据库自动迁移/模式创建功能已在配置中禁用。");
                return;
            }

            _logger.LogInformation("正在确保数据库模式...");
            using var connection = CreateConnection();
            await connection.OpenAsync();
            await CreateTablesIfNotExistInternalAsync(connection);
            _logger.LogInformation("数据库模式确保完成。");
        }

        // 从App.xaml.cs中迁移过来的表结构创建逻辑，调整为异步实现
        private async Task CreateTablesIfNotExistInternalAsync(SqliteConnection connection)
        {
            _logger.LogInformation("开始创建/验证数据库表结构...");

            // Create Users table
            var createUserTableSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT PRIMARY KEY, 
                    Username TEXT NOT NULL, 
                    Email TEXT,
                    ProfilePictureUrl TEXT, 
                    LastSyncTimestamp TEXT NOT NULL,
                    CustomStatus TEXT, 
                    LastOnlineTime TEXT
                );";
            await connection.ExecuteAsync(createUserTableSql);
            _logger.LogDebug("已确保 Users 表存在。");

            // Create Messages table
            var createMessagesTableSql = @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id TEXT PRIMARY KEY, 
                    SenderUserId TEXT NOT NULL, 
                    RecipientId TEXT NOT NULL,
                    RecipientType INTEGER NOT NULL, 
                    Content TEXT, 
                    MessageType INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL, 
                    Status INTEGER NOT NULL, 
                    IsEncrypted INTEGER NOT NULL DEFAULT 0,
                    SequenceNumber INTEGER NOT NULL, 
                    IsEdited INTEGER NOT NULL DEFAULT 0, 
                    EditedAt TEXT,
                    FileMetadataId TEXT, 
                    LocalFilePath TEXT
                );";
            await connection.ExecuteAsync(createMessagesTableSql);
            _logger.LogDebug("已确保 Messages 表存在。");

            // Create Friends table
            var createFriendsTableSql = @"
                CREATE TABLE IF NOT EXISTS Friends (
                    FriendshipId TEXT PRIMARY KEY, 
                    UserId TEXT NOT NULL, 
                    FriendUserId TEXT NOT NULL,
                    Remark TEXT, 
                    FriendGroupId TEXT, 
                    Status INTEGER NOT NULL, 
                    CreatedAt TEXT NOT NULL,
                    LastSyncTimestamp TEXT NOT NULL
                );";
            await connection.ExecuteAsync(createFriendsTableSql);
            _logger.LogDebug("已确保 Friends 表存在。");

            // Create FriendGroups table
            var createFriendGroupsTableSql = @"
                CREATE TABLE IF NOT EXISTS FriendGroups (
                    Id TEXT PRIMARY KEY, 
                    Name TEXT NOT NULL, 
                    ""Order"" INTEGER NOT NULL,
                    IsDefault INTEGER NOT NULL DEFAULT 0, 
                    LastSyncTimestamp TEXT NOT NULL
                );";
            await connection.ExecuteAsync(createFriendGroupsTableSql);
            _logger.LogDebug("已确保 FriendGroups 表存在。");

            // Create Groups table
            var createGroupsTableSql = @"
                CREATE TABLE IF NOT EXISTS Groups (
                    Id TEXT PRIMARY KEY, 
                    Name TEXT NOT NULL, 
                    Description TEXT, 
                    AvatarUrl TEXT,
                    OwnerUserId TEXT NOT NULL, 
                    CreatedAt TEXT NOT NULL, 
                    Announcement TEXT,
                    LastSyncTimestamp TEXT NOT NULL
                );";
            await connection.ExecuteAsync(createGroupsTableSql);
            _logger.LogDebug("已确保 Groups 表存在。");

            // Create GroupMembers table
            var createGroupMembersTableSql = @"
                CREATE TABLE IF NOT EXISTS GroupMembers (
                    GroupId TEXT NOT NULL, 
                    UserId TEXT NOT NULL, 
                    Role INTEGER NOT NULL,
                    JoinedAt TEXT NOT NULL, 
                    LastSyncTimestamp TEXT NOT NULL,
                    PRIMARY KEY (GroupId, UserId)
                );";
            await connection.ExecuteAsync(createGroupMembersTableSql);
            _logger.LogDebug("已确保 GroupMembers 表存在。");
            
            // Create SyncState table
            var createSyncStateTableSql = @"
                CREATE TABLE IF NOT EXISTS SyncState (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LastMessageSequence INTEGER NOT NULL DEFAULT 0,
                    LastSyncTimestamp TEXT NOT NULL
                );";
            await connection.ExecuteAsync(createSyncStateTableSql);
            _logger.LogDebug("已确保 SyncState 表存在。");

            // Insert initial sync state record if not exists
            var checkSyncStateSql = "SELECT COUNT(*) FROM SyncState;";
            long count = await connection.ExecuteScalarAsync<long>(checkSyncStateSql);
            if (count == 0)
            {
                var insertSyncStateSql = @"
                    INSERT INTO SyncState (LastMessageSequence, LastSyncTimestamp) 
                    VALUES (0, datetime('now', 'localtime'));";
                await connection.ExecuteAsync(insertSyncStateSql);
                _logger.LogInformation("已插入初始 SyncState 记录。");
            }
            _logger.LogInformation("数据库表结构创建/验证完成。");
        }

        // 保持现有的InitializeDatabaseAsync方法以确保兼容性，
        // 后续可能需要与新的EnsureDatabaseSchemaAsync合并或重构
        public async Task InitializeDatabaseAsync()
        {
            // TODO: 考虑在这里调用EnsureDatabaseSchemaAsync，或者在后续版本中将这两个方法合并
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            // 如果需要保留原有初始化逻辑，可以在这里继续实现，
            // 或者直接委托给新的CreateTablesIfNotExistInternalAsync方法
            await CreateTablesIfNotExistInternalAsync(connection);
        }

        private SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL查询失败: {Sql}", sql);
                throw;
            }
        }
 
        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null) where T : class // Added constraint and nullable return
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL查询首个结果失败: {Sql}", sql);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return await connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL语句失败: {Sql}", sql);
                throw;
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行SQL标量查询失败: {Sql}", sql);
                throw;
            }
        }

        public async Task<bool> ExecuteInTransactionAsync(Func<Task> action)
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                await action();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事务执行失败，执行回滚");
                transaction.Rollback();
                return false;
            }
        }

        // GroupDto CRUD Operations
        public async Task SaveGroupAsync(GroupDto group)
        {
            var sql = @"
            INSERT OR REPLACE INTO Groups (Id, Name, Description, AvatarUrl, Announcement, OwnerId, MemberCount, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Description, @AvatarUrl, @Announcement, @OwnerId, @MemberCount, @CreatedAt, @UpdatedAt);";
            await ExecuteAsync(sql, group);
            _logger.LogInformation("Saved group {GroupId}", group.Id);
        }

        public async Task<GroupDto?> GetGroupAsync(Guid groupId)
        {
            var sql = "SELECT * FROM Groups WHERE Id = @GroupId;";
            return await QueryFirstOrDefaultAsync<GroupDto>(sql, new { GroupId = groupId.ToString() });
        }

        public async Task<List<GroupDto>> GetUserGroupsAsync() // Assuming this means all groups, adjust if specific to a user
        {
            var sql = "SELECT * FROM Groups;";
            var groups = await QueryAsync<GroupDto>(sql);
            return groups.AsList();
        }

        public async Task<List<GroupDto>> GetUserJoinedGroupsAsync(Guid userId)
        {
            var sql = @"
                SELECT g.* FROM Groups g
                INNER JOIN GroupMembers gm ON g.Id = gm.GroupId
                WHERE gm.UserId = @UserId;";
            var groups = await QueryAsync<GroupDto>(sql, new { UserId = userId.ToString() });
            return groups.AsList();
        }

        public async Task DeleteGroupAsync(Guid groupId)
        {
            // Also delete associated members
            await DeleteGroupMembersAsync(groupId);
            var sql = "DELETE FROM Groups WHERE Id = @GroupId;";
            await ExecuteAsync(sql, new { GroupId = groupId.ToString() });
            _logger.LogInformation("Deleted group {GroupId} and its members", groupId);
        }

        public async Task SaveGroupsAsync(IEnumerable<GroupDto> groups)
        {
            // Using a transaction for batch operations is good practice
            await ExecuteInTransactionAsync(async () =>
            {
                foreach (var group in groups)
                {
                    await SaveGroupAsync(group); // Leverages the single save method
                }
            });
            _logger.LogInformation("Saved {Count} groups.", groups.Count());
        }

        // GroupMemberDto CRUD Operations
        public async Task SaveGroupMemberAsync(Guid groupId, GroupMemberDto member)
        {
            var sql = @"
            INSERT OR REPLACE INTO GroupMembers (GroupId, UserId, Username, Nickname, AvatarUrl, Role, JoinedAt)
            VALUES (@GroupId, @UserId, @Username, @Nickname, @AvatarUrl, @Role, @JoinedAt);";
            // Combine groupId with member properties for the database operation
            await ExecuteAsync(sql, new
            {
                GroupId = groupId.ToString(), // Ensure GroupId is passed as string if TEXT in DB
                member.UserId,
                member.Username,
                member.Nickname,
                member.AvatarUrl,
                member.Role,
                member.JoinedAt
            });
            _logger.LogInformation("Saved group member {UserId} for group {GroupId}", member.UserId, groupId);
        }

        public async Task SaveGroupMembersAsync(Guid groupId, IEnumerable<GroupMemberDto> members)
        {
            await ExecuteInTransactionAsync(async () =>
            {
                // Optional: Clear existing members for the group first if this is a full refresh
                // await DeleteGroupMembersAsync(groupId);
                foreach (var member in members)
                {
                    await SaveGroupMemberAsync(groupId, member); // Pass groupId explicitly
                }
            });
            _logger.LogInformation("Saved {Count} members for group {GroupId}", members.Count(), groupId);
        }

        public async Task<List<GroupMemberDto>> GetGroupMembersAsync(Guid groupId)
        {
            var sql = "SELECT * FROM GroupMembers WHERE GroupId = @GroupId;";
            var members = await QueryAsync<GroupMemberDto>(sql, new { GroupId = groupId.ToString() });
            return members.AsList();
        }

        public async Task DeleteGroupMemberAsync(Guid groupId, Guid userId) // Changed userId to Guid
        {
            var sql = "DELETE FROM GroupMembers WHERE GroupId = @GroupId AND UserId = @UserId;";
            await ExecuteAsync(sql, new { GroupId = groupId.ToString(), UserId = userId.ToString() }); // Ensure UserId is passed as string
            _logger.LogInformation("Deleted member {UserId} from group {GroupId}", userId, groupId);
        }

        public async Task DeleteGroupMembersAsync(Guid groupId)
        {
            var sql = "DELETE FROM GroupMembers WHERE GroupId = @GroupId;";
            await ExecuteAsync(sql, new { GroupId = groupId.ToString() });
            _logger.LogInformation("Deleted all members from group {GroupId}", groupId);
        }
    }
}