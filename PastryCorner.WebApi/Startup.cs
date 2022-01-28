using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using CacheManager.Core;
using Hangfire;
using Hangfire.Autofac;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using NServiceBus;
using PastryCorner.Contracts.Models;
using PastryCorner.Domain.Definitions;
using PastryCorner.Domain.Interfaces;
using PastryCorner.Domain.Models;
using PastryCorner.Infrastructure.Cache;
using PastryCorner.Infrastructure.Hubs;
using PastryCorner.Infrastructure.MongoSettings;
using PastryCorner.Infrastructure.Repositories;
using PastryCorner.WebApi.Extensions;
using PastryCorner.WebApi.Middleware;
using PastryCorner.WebApi.Models;
using Polly;
using Serilog;
using Serilog.Exceptions;

namespace PastryCorner.WebApi
{
    public class Startup
    {
        private IContainer _container;
        private Serilog.ILogger _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = Configuration.Get<Config>();

            var containerBuilder = GetAutoFacBuilder(config);
            containerBuilder = ConfigureSerilog(containerBuilder, config);

            ConfigureHealthChecks(services, config);
            ConfigureMvc(services, config);
            ConfigureSwagger(services);
            ConfigureHangfire(services, config);
            ConfigureAutoMapper(services);
            ConfigureMongo(services, config);
            ConfigureHttpClients(services);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddTransient<AuthorizationMiddleware>();
            //services.AddTransient<ClientMessagesMiddleware>();
            //services.AddTransient<NewRelicMiddleware>();
            ConfigureCache(services, config);
            ConfigureEndpoint(services, config);
            containerBuilder.Populate(services);
            _container = containerBuilder.Build();

            var serviceProvider = new AutofacServiceProvider(_container);
            GlobalConfiguration.Configuration.UseActivator(new AutofacJobActivator(_container));

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "PastryCorner.WebApi", Version = "v2" });
            });

            //var settings = MongoClientSettings.FromConnectionString("mongodb+srv://admin:admin@devcluster.grraf.mongodb.net/PastryCorner?retryWrites=true&w=majority");
            //var client = new MongoClient(settings);
            //var database = client.GetDatabase("PastryCorner");

            //var feedbackCollection = database.GetCollection<Feedback>("feedback");
            //var document = feedbackCollection
            //        .Find(f => true)
            //        .FirstOrDefault();

            //Console.WriteLine(document);
        }

        private void ConfigureEndpoint(IServiceCollection services, Config config)
        {
            var endpointName = "PastryCorner.Endpoint";
            var endpointConfiguration = new EndpointConfiguration("PastryCorner.Endpoint");
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.ConnectionString(config.ConnectionStrings.NServiceBusTransportConnectionString);
            transport.UseConventionalRoutingTopology();
            transport.Routing().RouteToEndpoint(Assembly.Load("PastryCorner.Contracts"), endpointName);
            NServiceBus.Logging.LogManager.Use<NServiceBus.Serilog.SerilogFactory>();
            endpointConfiguration.SendOnly();
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.Conventions()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.StartsWith("PastryCorner.") &&
                                         t.Namespace.EndsWith(".Commands"))
                .DefiningMessagesAs(t => t.Namespace != null && t.Namespace.StartsWith("PastryCorner.") &&
                                         t.Namespace.EndsWith(".Messages"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("PastryCorner.") &&
                                       t.Namespace.EndsWith(".Events"));
            var endpoint = NServiceBus.Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
            services.AddSingleton<IMessageSession>(endpoint);
        }

        private void ConfigureCache(IServiceCollection services, Config config)
        {
            var userCache = new UserCacheRepository(new DbConnectionFactory(config.ConnectionStrings.MongoConnectionString));
            services.AddSingleton<IUserCacheRepository>(userCache);

        }

        private void ConfigureHttpClients(IServiceCollection services)
        {
            services.AddHttpClient("RecommendationsApi", client => { client.Timeout = TimeSpan.FromSeconds(3); })
                //.AddHttpMessageHandler<TokenDelegatingHandler>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // ignore errors from self signed certs
                    })
                .AddTransientHttpErrorPolicy(builder =>
                    builder.AdvancedCircuitBreakerAsync(0.5, TimeSpan.FromSeconds(10), 10, TimeSpan.FromMinutes(1)));
        }

        private void ConfigureMongo(IServiceCollection services, Config config)
        {
            var mongoClient = new MongoClient(config.ConnectionStrings.MongoConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(config.MongoDatabase);

            services.AddSingleton<IMongoClient>(mongoClient);
            services.AddTransient(context => mongoDatabase);
            services.AddTransient(context => mongoDatabase.GetCollection<Feedback>(Collections.AppFeedback));
            StorageBsonMapping.RegisterClassMaps();
            StorageBsonIndex.CreateIndexesAsync(mongoDatabase).ConfigureAwait(false);
        }

        private void ConfigureHangfire(IServiceCollection services, Config config)
        {
            services.AddHangfire(x =>
                x.UseMongoStorage(config.ConnectionStrings.MongoConnectionString, config.MongoDatabase));
            services.AddSingleton<IBackgroundJobClient>(x => new BackgroundJobClient());
            //services.AddSingleton<ILoadIndexer>(x => new PastrySearch(new ElasticClient(
            //    PastrySearchFactory.ElasticClientFactory.GetIndexConnection(new Uri(appConfig.PastryCornerApiElastisearchUri), _esHeaders)),
            //    appConfig.PastryCorerEsAliasName, _logger));
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PastryCorner.WebApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();
            //app.UseAuthorizationMiddleware();
            //app.UseNewRelicMiddleware();
            //app.UseClientMessagesMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        #region Configuration Methods

        private static ContainerBuilder GetAutoFacBuilder(Config config)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(
                Assembly.Load("PastryCorner.Infrastructure"),
                Assembly.Load("PastryCorner.Domain")).Where(t => !string.IsNullOrWhiteSpace(t.Namespace))
                .WithParameters(new Parameter[]
                {
                    new NamedParameter("enableElasticLog", config.Features.EnableElasticLog),
                    new NamedParameter("enablePurchaseReport", config.Features.EnablePurchaseReport),
                })
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.ConnectionStrings).SingleInstance();
            builder.RegisterInstance(config.Features).SingleInstance();
            builder.RegisterAssemblyTypes(Assembly.Load("PastryCorner.Domain"))
                .Where(t => !string.IsNullOrWhiteSpace(t.Namespace))
                .WithParameters(new Parameter[]
                {
                    new NamedParameter("enableExceptionEmail", config.Features.EnableExceptionEmail),
                    new NamedParameter("enableClientDeprecationNotification", config.Features.EnableClientDeprecationNotification)
                })
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<UserCache>().AsSelf().AsImplementedInterfaces().SingleInstance();
            //SingalR Hubs
            builder.RegisterHubs(Assembly.Load("PastryCorner.Domain"), Assembly.Load("PastryCorner.Infrastructure"));
            builder.RegisterType<NotifyHub>().AsSelf().SingleInstance();
            
            //cache manager
            //builder.RegisterInstance<CacheSettings>(config.CacheSettings).SingleInstance();
            var pastryViewerCache = GetRedisCache<List<UserInfo>>(config);
            builder.RegisterInstance(pastryViewerCache).SingleInstance();

            //Handlers
            //builder.RegisterType<TokenDelegatingHandler>();
            return builder;
        }

        private static ICacheManager<T> GetRedisCache<T>(Config config)
        {
            var serializationInfo = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };
            var redisString = config.ConnectionStrings.RedisConnectionString.Split(',');
            var redisHost = redisString.First().Split(':').First();
            var redisPort = redisString.First().Split(':').Last();
            var redisPass = Regex.Match(config.ConnectionStrings.RedisConnectionString, "password=([^,]*)");

            var cacheConfig = CacheManager.Core.ConfigurationBuilder.BuildConfiguration(settings =>
            {
                settings
                    .WithMicrosoftMemoryCacheHandle()
                    .And
                    .WithJsonSerializer(serializationInfo, serializationInfo)
                    .WithRedisConfiguration("redis", conf =>
                    {
                        if (redisPass.Success)
                        {
                            conf.WithAllowAdmin()
                                .WithDatabase(0)
                                .WithEndpoint(redisHost, Convert.ToInt32(redisPort))
                                .WithPassword(redisPass.Captures[0].Value.Substring(9))
                                .WithSsl();
                        }
                        else
                        {
                            conf.WithAllowAdmin()
                                .WithDatabase(0)
                                .WithEndpoint(redisHost, Convert.ToInt32(redisPort));
                        }
                    })
                    .WithMaxRetries(100)
                    .WithRetryTimeout(50)
                    .WithRedisBackplane("redis")
                    .WithRedisCacheHandle("redis");
            });
            return CacheFactory.FromConfiguration<T>(cacheConfig);
        }

        private ContainerBuilder ConfigureSerilog(ContainerBuilder container, Config config)
        {
            _logger = ConfigureLogger(config).CreateLogger();
            container.RegisterInstance(_logger).As<Serilog.ILogger>();
            return container;
        }

        private LoggerConfiguration ConfigureLogger(Config config)
        {
            var loggerConfig = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PastryCorner")
                .WriteTo.Trace();
                //.WriteTo.File(config.Serilog.RollingFilePathFormat, outputTemplate: config.Serilog.RollingFileOutputTemplate, rollingInterval: RollingInterval.Day)
                //.WriteTo.Elasticsearch(GetElasticsearchSinkOptions(config));

            switch (config.MinimumLogLevel)
            {
                case "Debug":
                    loggerConfig.MinimumLevel.Debug();
                    break;
                case "Information":
                    loggerConfig.MinimumLevel.Information();
                    break;
                case "Warning":
                    loggerConfig.MinimumLevel.Warning();
                    break;
                case "Error":
                    loggerConfig.MinimumLevel.Error();
                    break;
                case "Fatal":
                    loggerConfig.MinimumLevel.Fatal();
                    break;
                default:
                    loggerConfig.MinimumLevel.Debug();
                    break;
            }

            return loggerConfig;
        }

        private static void ConfigureMvc(IServiceCollection services, Config config)
        {
            services.AddMvc().AddControllersAsServices();
            services.AddControllers();
        }

        private static void ConfigureAutoMapper(IServiceCollection services)
        {
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PastryCorner API", Version = "v1" });
            });
        }

        private static void ConfigureHealthChecks(IServiceCollection services, Config config)
        {
            //Databases Health Checks
            services.AddHealthChecks()
                .AddMongoDb(config.ConnectionStrings.MongoConnectionString, name: "MongoDB", tags: new[] { "ready" });
            services.AddHealthChecksUI().AddInMemoryStorage();
        }

        
        #endregion

    }
}
