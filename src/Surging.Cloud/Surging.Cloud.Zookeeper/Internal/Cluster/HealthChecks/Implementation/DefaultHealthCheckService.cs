using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;
using Surging.Cloud.CPlatform.Utilities;
using static org.apache.zookeeper.Watcher;

namespace Surging.Cloud.Zookeeper.Internal.Cluster.HealthChecks.Implementation
{
    public class DefaultHealthCheckService : IHealthCheckService
    {
        private readonly int _timeout = 10000;
        private readonly Timer _timer;
        private readonly ConcurrentDictionary<string, MonitorEntry> _dictionary = new ConcurrentDictionary<string, MonitorEntry>();
        private readonly ILogger<DefaultHealthCheckService> _logger;
        #region Implementation of IHealthCheckService
        public DefaultHealthCheckService(ILogger<DefaultHealthCheckService> logger)
        {
            _logger = logger;
            var timeSpan = TimeSpan.FromSeconds(60);
            _timer = new Timer(async s =>
            {
                await Check(_dictionary.ToArray().Select(i => i.Value));
            }, null, timeSpan, timeSpan);
        }

        public async Task<bool> IsHealth(string conn)
        {
            MonitorEntry entry;
            var isHealth = !_dictionary.TryGetValue(conn, out entry) ? await Check(conn) : entry.Health;
            return isHealth;
        }

        public async Task Monitor(string conn)
        {
            if (!_dictionary.TryGetValue(conn, out MonitorEntry entry)) 
            {
                entry = new MonitorEntry(conn, await Check(conn));
                _dictionary.GetOrAdd(conn, entry);
            }
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            _timer.Dispose();
        }
        #endregion

        #endregion Implementation of IDisposable

        #region Private Method

        private async Task Check(MonitorEntry entry)
        {
            ZookeeperClient zookeeperClient = null;
            try
            {
                var options = new ZookeeperClientOptions(entry.Connection) 
                {
                    ConnectionTimeout = TimeSpan.FromMilliseconds(_timeout)
                };
                zookeeperClient = new ZookeeperClient(options);
                entry.Health = true;
            }
            catch (Exception)
            {
                entry.Health = false;
            }
            finally
            {
                if (zookeeperClient != null) 
                {
                    zookeeperClient.Dispose();
                }
            };
        }

        private async Task<bool> Check(string conn)
        {
            ZookeeperClient zookeeperClient = null;
            try
            {
                var options = new ZookeeperClientOptions(conn)
                {
                    ConnectionTimeout = TimeSpan.FromMilliseconds(_timeout)
                };
                zookeeperClient = new ZookeeperClient(options);
                return zookeeperClient.WaitForKeeperState(Event.KeeperState.SyncConnected, TimeSpan.FromMilliseconds(_timeout));
            }
            catch (Exception ex)
            {
                _logger.LogError("服务注册中心连接失败,原因:"+ ex.Message);
                return false;
            }
            finally
            {
                if (zookeeperClient != null)
                {
                    zookeeperClient.Dispose();
                }
            };
        }

        private async Task Check(IEnumerable<MonitorEntry> entries)
        {
            foreach (var entry in entries) 
            {
                ZookeeperClient zookeeperClient = null;
                try
                {
                    var options = new ZookeeperClientOptions(entry.Connection)
                    {
                        ConnectionTimeout = TimeSpan.FromMilliseconds(_timeout),
                    };
                    zookeeperClient = new ZookeeperClient(options);
                    entry.UnhealthyTimes = 0;
                    entry.Health = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError("服务注册中心连接失败,原因:" + ex.Message);
                    entry.UnhealthyTimes++;
                    entry.Health = false;
                }
                finally
                {
                    if (zookeeperClient != null)
                    {
                        zookeeperClient.Dispose();
                    }
                };
            }
        }

        #endregion Private Method

        #region Help Class

        protected class MonitorEntry
        {
            public MonitorEntry(string conn, bool health = true)
            {
                Connection = conn;
                Health = health;

            }

            public int UnhealthyTimes { get; set; }

            public string Connection { get; set; }
            public bool Health { get; set; }
        }

        #endregion Help Class
    }
}

