using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Surging.Cloud.CPlatform.Cache;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.CPlatform.Mqtt;
using Surging.Cloud.CPlatform.Routing;
using Surging.Cloud.CPlatform.Runtime.Client;
using Surging.Cloud.CPlatform.Runtime.Server;
using Surging.Cloud.CPlatform.Serialization;
using Surging.Cloud.CPlatform.Support;
using Surging.Cloud.Zookeeper.Configurations;
using Surging.Cloud.Zookeeper.Internal;
using Surging.Cloud.Zookeeper.Internal.Cluster.HealthChecks;
using Surging.Cloud.Zookeeper.Internal.Cluster.HealthChecks.Implementation;
using Surging.Cloud.Zookeeper.Internal.Cluster.Implementation.Selectors;
using Surging.Cloud.Zookeeper.Internal.Cluster.Implementation.Selectors.Implementation;
using Surging.Cloud.Zookeeper.Internal.Implementation;

namespace Surging.Cloud.Zookeeper
{
   public static class ServiceHostBuilderExtensions
    {
        public static IHostBuilder UseZookeeper(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
           
                var configInfo = new ConfigInfo(null);
                containerBuilder.RegisterInstance(GetConfigInfo(configInfo));
                containerBuilder.RegisterType<ZookeeperRandomAddressSelector>().As<IZookeeperAddressSelector>().SingleInstance();
                containerBuilder.RegisterType<DefaultHealthCheckService>().As<IHealthCheckService>().SingleInstance();
                containerBuilder.RegisterType<DefaultZookeeperClientProvider>().As<IZookeeperClientProvider>();
                containerBuilder.RegisterType<ZooKeeperServiceRouteManager>().As<IServiceRouteManager>();
                containerBuilder.RegisterType<ZooKeeperMqttServiceRouteManager>().As<IMqttServiceRouteManager>();
                containerBuilder.RegisterType<ZookeeperServiceCacheManager>().As<IServiceCacheManager>();
                containerBuilder.RegisterType<ZookeeperServiceCommandManager>().As<IServiceCommandManager>();
                containerBuilder.RegisterType<ZooKeeperServiceSubscribeManager>().As<IServiceSubscribeManager>();

            });

        }
        private static ConfigInfo GetConfigInfo(ConfigInfo config)
        {
            ZookeeperOption option = null;
            var section = CPlatform.AppConfig.GetSection("Zookeeper");
            if (section.Exists())
                option = section.Get<ZookeeperOption>();
            else if (AppConfig.Configuration != null)
                option = AppConfig.Configuration.Get<ZookeeperOption>();
            if (option != null)
            {
                var sessionTimeout = config.SessionTimeout.TotalSeconds;
                var connectionTimeout = config.ConnectionTimeout.TotalSeconds;
                var operatingTimeout = config.OperatingTimeout.TotalSeconds;
                if (option.SessionTimeout > 0)
                {
                    sessionTimeout = option.SessionTimeout;
                }
                if (option.ConnectionTimeout > 0)
                {
                    connectionTimeout = option.ConnectionTimeout;
                }
                if (option.OperatingTimeout > 0)
                {
                    operatingTimeout = option.OperatingTimeout;
                }
                config = new ConfigInfo(
                    option.ConnectionString,
                    TimeSpan.FromSeconds(sessionTimeout),
                    TimeSpan.FromSeconds(connectionTimeout),
                    TimeSpan.FromSeconds(operatingTimeout),
                    option.RoutePath ?? config.RoutePath,
                    option.SubscriberPath ?? config.SubscriberPath,
                    option.CommandPath ?? config.CommandPath,
                    option.CachePath ?? config.CachePath,
                    option.MqttRoutePath ?? config.MqttRoutePath,
                    option.ChRoot ?? config.ChRoot,
                    option.ReloadOnChange != null ? bool.Parse(option.ReloadOnChange) :
                    config.ReloadOnChange,
                    option.EnableChildrenMonitor != null ? bool.Parse(option.EnableChildrenMonitor) :
                    config.EnableChildrenMonitor
                   );
            }
            return config;
        }

    }
}
