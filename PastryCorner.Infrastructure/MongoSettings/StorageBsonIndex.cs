
namespace PastryCorner.Infrastructure.MongoSettings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using MongoDB.Driver;
    using PastryCorner.Contracts.Models;
    using PastryCorner.Domain.Definitions;
    using PastryCorner.Domain.Models;

    [ExcludeFromCodeCoverage]
    public static class StorageBsonIndex
    {
        public static async Task CreateIndexesAsync(IMongoDatabase database)
        {
            var feedbackCollection = database.GetCollection<Feedback>(Collections.AppFeedback);
            await feedbackCollection.Indexes.CreateOneAsync(new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys.Ascending("expirationDate"),
                new CreateIndexOptions { Name = "TTL", ExpireAfter = new TimeSpan(0, 0, 0) })).ConfigureAwait(false);
            await feedbackCollection.Indexes.CreateOneAsync(new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys.Ascending("bazookaTruckId"))).ConfigureAwait(false);
            await feedbackCollection.Indexes.CreateOneAsync(new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys.Descending(f => f.CreateDateUtc).Ascending(f => f.UserId),
                new CreateIndexOptions
                {
                    Name = "Feedback_CreateDateUtc_UserId_idx"
                })).ConfigureAwait(false);


            var userCollection = database.GetCollection<UserInfo>(Collections.Users);

            await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserInfo>(
                Builders<UserInfo>.IndexKeys.Ascending("expirationDate"),
                new CreateIndexOptions
                {
                    Name = "TTL",
                    ExpireAfter = new TimeSpan(0, 0, 0)
                })).ConfigureAwait(false);
        }
    }
}
