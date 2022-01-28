using System;
using System.Collections.Generic;
using System.Text;
using PastryCorner.Domain.Interfaces;

namespace PastryCorner.Infrastructure.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Dapper;
    using PastryCorner.Contracts.Models;

    public class UserCacheRepository : RepositoryBase, IUserCacheRepository
    {
        #region Properties

        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24.0);
        private DateTime? _lastLoaded;
        private bool _isCaching;
        private Dictionary<int, UserInfo> _userDictionary = new Dictionary<int, UserInfo>();
        private const string QueryColumns = @"SU.UserId AS UserId, SU.FirstName AS FirstName, SU.LastName AS LastName, SU.Code AS Code, SU.PhoneNumber AS PhoneNumber";
        private const string QueryFromTables = @"[dbo].User SU";

        private bool IsCacheExpired => _lastLoaded == null || DateTime.Now - _lastLoaded.Value > Lifetime;

        #endregion

        #region Constructor
        public UserCacheRepository(IDbConnectionFactory factory) : base(factory) { }

        #endregion

        public async Task CacheUsersAsync()
        {
            var users = await QueryUsersAsync(string.Empty).ConfigureAwait(false);
            _userDictionary = new Dictionary<int, UserInfo>(users.ToDictionary(x => x.UserId, x => x));
        }

        public async Task<UserInfo> GetUserByIdAsync(int userId)
        {
            await CheckCacheExpiry().ConfigureAwait(false);
            if (_userDictionary.ContainsKey(userId))
                return _userDictionary[userId];

            var queryString = $@"
                SELECT  {QueryColumns}
                FROM    {QueryFromTables}
                WHERE   SU.SystemUserId = @userId";

            var parameters = new DynamicParameters();
            parameters.Add("userId", userId);

            var userInfo = await WithConnectionAsync(async (connection, transaction) =>
            {
                return (await connection.QueryFirstOrDefaultAsync<UserInfo>(queryString, parameters, transaction).ConfigureAwait(false));
            }).ConfigureAwait(false);

            _userDictionary[userId] = userInfo;
            return userInfo;

        }

        public async Task<Dictionary<int, UserInfo>> GetUserByIdsAsync(List<int> userIds)
        {
            await CheckCacheExpiry().ConfigureAwait(false);
            var userDictionary = _userDictionary.Where(x => userIds.Contains(x.Key)).ToDictionary(x => x.Key, v => v.Value);

            var unCachedUserIds = userIds.Except(userDictionary.Keys);

            if (unCachedUserIds.Any())
            {
                var users = await QueryUsersAsync($"WHERE SU.UserId IN ({String.Join(",", unCachedUserIds)})").ConfigureAwait(false);

                foreach (var userInfo in users)
                {
                    _userDictionary[userInfo.UserId] = userInfo;
                    userDictionary[userInfo.UserId] = userInfo;
                }
            }

            return userDictionary;
        }

        public Task<List<UserInfo>> QueryUsersAsync(string where)
        {
            var usersSql = $@"
                   SELECT {QueryColumns}
                   FROM   {QueryFromTables} {where};";

            return WithConnectionAsync(async (connection, transaction) =>
                (await connection.QueryAsync<UserInfo>(usersSql, null, transaction).ConfigureAwait(false)).ToList());
        }

        private async Task CheckCacheExpiry()
        {
            if (IsCacheExpired && !_isCaching)
            {
                _isCaching = true;
                await CacheUsersAsync().ConfigureAwait(false);
                _lastLoaded = DateTime.Now;
                _isCaching = false;
            }
        }
    }
}
