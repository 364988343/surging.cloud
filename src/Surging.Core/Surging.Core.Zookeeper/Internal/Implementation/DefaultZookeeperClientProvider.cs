﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using org.apache.zookeeper;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;
using Surging.Core.CPlatform;
using Surging.Core.CPlatform.Address;
using Surging.Core.CPlatform.Exceptions;
using Surging.Core.CPlatform.Runtime.Client.Address.Resolvers.Implementation.Selectors;
using Surging.Core.Zookeeper.Configurations;
using Surging.Core.Zookeeper.Internal.Cluster.HealthChecks;
using Surging.Core.Zookeeper.Internal.Cluster.Implementation.Selectors;
using Level = Microsoft.Extensions.Logging.LogLevel;

namespace Surging.Core.Zookeeper.Internal.Implementation
{
    public class DefaultZookeeperClientProvider : IZookeeperClientProvider
    {
        private ConfigInfo _config;
        private readonly IHealthCheckService _healthCheckService;
        private readonly IZookeeperAddressSelector _zookeeperAddressSelector;
        private readonly ILogger<DefaultZookeeperClientProvider> _logger;
        private readonly ConcurrentDictionary<string, IAddressSelector> _addressSelectors = new ConcurrentDictionary<string, IAddressSelector>();
        private readonly ConcurrentDictionary<string, IZookeeperClient> _zookeeperClients = new ConcurrentDictionary<string, IZookeeperClient>();


        public DefaultZookeeperClientProvider(ConfigInfo config, IHealthCheckService healthCheckService, IZookeeperAddressSelector zookeeperAddressSelector,
      ILogger<DefaultZookeeperClientProvider> logger)
        {
            _config = config;
            _healthCheckService = healthCheckService;
            _zookeeperAddressSelector = zookeeperAddressSelector;
            _logger = logger;
        }


        public async Task<IZookeeperClient> GetZooKeeperClient()
        {
            var addr = await _zookeeperAddressSelector.SelectConnectionAsync(new AddressSelectContext
            {
                Descriptor = new ServiceDescriptor { Id = nameof(DefaultZookeeperClientProvider) },
                Connections = _config.Addresses
            });
           
            return CreateZooKeeper(addr);
        }

        protected IZookeeperClient CreateZooKeeper(string conn)
        {
            if (_zookeeperClients.TryGetValue(conn, out IZookeeperClient zookeeperClient) 
                && zookeeperClient.WaitForKeeperState(Watcher.Event.KeeperState.SyncConnected, zookeeperClient.Options.OperatingTimeout))
            {
                return zookeeperClient;
            }
            else 
            {
                var options = new ZookeeperClientOptions(conn)
                {
                    ConnectionTimeout = _config.ConnectionTimeout,
                    SessionTimeout = _config.SessionTimeout,
                    OperatingTimeout = _config.OperatingTimeout
                };
                zookeeperClient = new ZookeeperClient(options);
                _zookeeperClients.AddOrUpdate(conn, zookeeperClient, (k, v) => zookeeperClient);
                return zookeeperClient;
            }
        }

        public async Task<IEnumerable<IZookeeperClient>> GetZooKeeperClients()
        {
            var result = new List<IZookeeperClient>();
            foreach (var address in _config.Addresses)
            {
                result.Add(CreateZooKeeper(address));
            }
            return result;
        }

        public void Dispose()
        {
            if (_zookeeperClients.Any())
            {
                foreach (var client in _zookeeperClients)
                {
                    client.Value.Dispose();
                }
            }
        }
    }
}
