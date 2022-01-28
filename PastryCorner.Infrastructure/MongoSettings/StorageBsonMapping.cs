
namespace PastryCorner.Infrastructure.MongoSettings
{
    using System.Diagnostics.CodeAnalysis;
    using MongoDB.Bson.Serialization;
    using MongoDB.Bson.Serialization.Conventions;
    using PastryCorner.Contracts.Models;
    using PastryCorner.Domain.Models;

    [ExcludeFromCodeCoverage]
    public static class StorageBsonMapping
    {
        public static void RegisterClassMaps()
        {
            var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("camelCase", conventionPack, t => true);

            if (!BsonClassMap.IsClassMapRegistered(typeof(Feedback)))
                BsonClassMap.RegisterClassMap<Feedback>(mapper =>
                {
                    mapper
                        .AutoMap();

                    mapper
                        .SetIgnoreExtraElements(true);

                    // nested document mapping as below
                    //mapper
                    //    .MapMember(c => c.PastryStates)
                    //    .SetElementName("pastryStates");

                });

            if (!BsonClassMap.IsClassMapRegistered(typeof(UserInfo)))
                BsonClassMap.RegisterClassMap<UserInfo>(mapper =>
                {
                    mapper
                        .AutoMap();

                    mapper
                        .SetIgnoreExtraElements(true);
                });

        }
    }
}
