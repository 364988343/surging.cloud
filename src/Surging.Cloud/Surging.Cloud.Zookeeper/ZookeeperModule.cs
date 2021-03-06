﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using System;

namespace Surging.Cloud.Zookeeper
{
    public class ZookeeperModule : EnginePartModule
    {
        protected override void RegisterBuilder(ContainerBuilderWrapper builder)
        {
           
            var configInfo = new ConfigInfo(null);
            builder.RegisterInstance(GetConfigInfo(configInfo));
            builder.RegisterType<ZookeeperRandomAddressSelector>().As<IZookeeperAddressSelector>().SingleInstance();
            builder.RegisterType<DefaultHealthCheckService>().As<IHealthCheckService>().SingleInstance();
            builder.RegisterType<DefaultZookeeperClientProvider>().As<IZookeeperClientProvider>();
            builder.RegisterType<ZooKeeperServiceRouteManager>().As<IServiceRouteManager>();
            builder.RegisterType<ZooKeeperMqttServiceRouteManager>().As<IMqttServiceRouteManager>();
            builder.RegisterType<ZookeeperServiceCacheManager>().As<IServiceCacheManager>();
            builder.RegisterType<ZookeeperServiceCommandManager>().As<IServiceCommandManager>();
            builder.RegisterType<ZooKeeperServiceSubscribeManager>().As<IServiceSubscribeManager>();
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
