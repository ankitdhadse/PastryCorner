using System;

namespace PastryCorner.Configuration.Common
{
    public class AutofacConfiguration
    {
        private static ContainerBuilder GetAutoFacBuilder(Config config)
        {
            var builder = new ContainerBuilder();
            var casmatAliasName = config.CasMatEsAliasName;
            var featureFlagClient = new FeatureFlagsClient(config.ExternalApis.FeatureFlagUri.Url,
                new CacheOptions()
                {
                    EnableCache = true,
                    CachedItemsLifeSpan = new TimeSpan(0, 0, config.Features.FeatureFlagCacheMinutes, 0)
                });
            var tokenService = new TokenService(new Uri(config.ExternalApis.IdentityServerBaseUri.Url), config.IdentityServer.RequireHttpsMetadata,
                config.IdentityServer.ClientId, config.IdentityServer.ClientSecret);
            builder.RegisterAssemblyTypes(Assembly.Load("Coyote.Procurement.CasMat.Services.Storage"),
                    Assembly.Load("Coyote.Procurement.CasMat.Services.Index"),
                    Assembly.Load("Coyote.Procurement.CasMat.Services.Client"),
                    Assembly.Load("Coyote.Procurement.CasMat.Services.Udp")).Where(t => !string.IsNullOrWhiteSpace(t.Namespace))
                .WithParameters(new Parameter[]
                {
                    new NamedParameter("elasticsearchAliasName", casmatAliasName),
                    new NamedParameter("casMatApiElasticSearchUri", new Uri(config.CasMatApiElastisearchUri)),
                    new NamedParameter("recommendationsApiServiceBaseUri", new Uri(config.ExternalApis.RecommendationsApiBaseUri.Url)),
                    new NamedParameter("offerApiServiceBaseUri", new Uri(config.ExternalApis.OfferApiServiceBaseUri.Url)),
                    new NamedParameter("realtimeInfoWebApiBaseUri", new Uri(config.ExternalApis.RealtimeInfoWebApiBaseUri.Url)),
                    new NamedParameter("businessEventsPublisherWebApiBaseUri",
                        new Uri(config.ExternalApis.BusinessEventsPublisherWebApiBaseUri.Url)),
                    new NamedParameter("esLoadIndexRebuildVersion", config.EsLoadIndexRebuildVersion),
                    new NamedParameter("bookItNowApiBaseUri", new Uri(config.ExternalApis.BookItNowApiBaseUri.Url)),
                    new NamedParameter("enableCustomerRateVisibility",config.Features.EnableCustomerRateVisibility),
                    new NamedParameter("enableActiveLoadsRtu", config.Features.EnableActiveLoadsRtu),
                    new NamedParameter("businessIntelligenceApiServiceBaseUri", new Uri(config.ExternalApis.BusinessIntelligenceApiBaseUri.Url + BusinessIntelligenceApiRoute)),
                    new NamedParameter("carrierTrackingPreferencesWebApiBaseUri", new Uri(config.ExternalApis.CarrierTrackingPreferencesWebApiBaseUri.Url + CarrierTrackingPreferencesApiRoute)),
                    new NamedParameter("elasticsearchNodeUris", config.Serilog.ElasticsearchNodeUris),
                    new NamedParameter("deleteLogIndexInterval", config.DeleteLogIndexInterval),//Interval to delete kibana indices in days
                    new NamedParameter("emailWebApiBaseUri", new Uri(config.ExternalApis.EmailWebApiBaseUri.Url)),
                    new NamedParameter("exceptionEmailReceiver", config.ExceptionEmailReceiver),
                    new NamedParameter("carrierSearchBaseUri", config.ExternalApis.CarrierSearchBaseUri.Url),
                    new NamedParameter("featureFlagClient", featureFlagClient),
                    new NamedParameter("carrierTierApiServiceBaseUri", new Uri(config.ExternalApis.CarrierTierApiBaseUri.Url + $"api/{config.CarrierTierApiVersion}/")),
                    new NamedParameter("barkHubBaseUri", new Uri(config.ExternalApis.BarkHubBaseUri.Url)),
                    new NamedParameter("executionCommandBaseUri", new Uri(config.ExternalApis.ExecutionCommandBaseUri.Url)),
                    new NamedParameter("identityServerBaseUri", new Uri(config.ExternalApis.IdentityServerBaseUri.Url)),
                    new NamedParameter("loadRequirementBaseUri", new Uri(config.ExternalApis.LoadRequirementBaseUri.Url)),
                    new NamedParameter("locationServiceBaseUri", new Uri(config.ExternalApis.LocationServiceBaseUri.Url)),
                    new NamedParameter("secretsWebApiBaseUri", new Uri(config.ExternalApis.SecretsWebApiBaseUri.Url)),
                    new NamedParameter("userCarrierDefaultExpirationInDays", config.UserCarrierDefaultExpirationInDays),
                    new NamedParameter("enableTruckDataUpdation", config.Features.EnableTruckDataUpdation),
                    new NamedParameter("marketCostWebApiBaseUri", config.ExternalApis.MarketCostWebApiBaseUri.Url + $"api/{config.MarketCostApiVersion}/Commissions/GetEstimatedCommission"),
                    new NamedParameter("maxPayServiceBaseUri", new Uri(config.ExternalApis.MaxPayWebApiBaseUri.Url))
                })
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.ClientSettings).SingleInstance();
            builder.RegisterInstance(config.MilesSettings).SingleInstance();
            builder.RegisterAssemblyTypes(Assembly.Load("Coyote.Procurement.CasMat.Services.Domain"))
                .Where(t => !string.IsNullOrWhiteSpace(t.Namespace))
                .WithParameters(new Parameter[]
                {
                    new NamedParameter("serviceUserId", ServiceUserId),
                    new NamedParameter("exceptionEmailReceiver", config.ExceptionEmailReceiver),
                    new NamedParameter("userCarrierDefaultExpirationInDays", config.UserCarrierDefaultExpirationInDays),
                    new NamedParameter("enableTruckDataUpdation", config.Features.EnableTruckDataUpdation)
                })
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterInstance(config.Features).SingleInstance();
            builder.RegisterType<LockingCache>().AsSelf().AsImplementedInterfaces().SingleInstance();
            //SingalR Hubs
            builder.RegisterHubs(Assembly.Load("Coyote.Procurement.CasMat.Services.Domain"));
            builder.RegisterType<NotifyHub>().AsSelf().SingleInstance();
            builder.RegisterInstance<ICustomerRateVisibilityApiClient>(new CustomerRateVisibilityApiClient(new Uri(config.ExternalApis.CustomerRateVisibilityApiBaseUri.Url))).SingleInstance();
            builder.RegisterInstance<IMaxPayApiClient>(new MaxPayApiClient(new Uri(config.ExternalApis.MaxPayWebApiBaseUri.Url))).SingleInstance();

            //cache manager
            builder.RegisterInstance<CacheSettings>(config.CacheSettings).SingleInstance();
            var carrierViewerCache = GetRedisCache<List<CarrierViewerInfo>>(config);
            builder.RegisterInstance(carrierViewerCache).SingleInstance();

            var postalCodeCache = GetRedisCache<CityPostalCodeQueryResponse>(config);
            builder.RegisterInstance(postalCodeCache).SingleInstance();
            builder.RegisterType<PostalCodesRedisCache>().AsSelf().AsImplementedInterfaces().SingleInstance();

            var milesCache = GetRedisCache<double>(config);
            builder.RegisterInstance(milesCache).SingleInstance();
            builder.RegisterType<MilesRedisCache>().AsSelf().AsImplementedInterfaces().SingleInstance();

            //Single Instance
            builder.RegisterInstance(tokenService).SingleInstance();
            builder.RegisterInstance(config.Client).SingleInstance();

            //Handlers
            builder.RegisterType<TokenDelegatingHandler>();
            return builder;
        }
    }
}
