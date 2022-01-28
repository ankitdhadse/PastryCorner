
namespace PastryCorner.Domain.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using PastryCorner.Contracts.Models;

    public interface IUserCacheRepository
    {
        Task<UserInfo> GetUserByIdAsync(int userId);

        Task<Dictionary<int, UserInfo>> GetUserByIdsAsync(List<int> userIds);

        Task CacheUsersAsync();
    }
}
