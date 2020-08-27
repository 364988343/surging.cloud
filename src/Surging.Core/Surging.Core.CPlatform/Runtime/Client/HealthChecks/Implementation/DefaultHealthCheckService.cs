﻿using Microsoft.Extensions.Logging;
using Surging.Core.CPlatform.Address;
using Surging.Core.CPlatform.Exceptions;
using Surging.Core.CPlatform.Routing;
using Surging.Core.CPlatform.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Surging.Core.CPlatform.Runtime.Client.HealthChecks.Implementation
{
    /// <summary>
    /// 默认健康检查服务(每10秒会检查一次服务状态，在构造函数中添加服务管理事件) 
    /// </summary>
    public class DefaultHealthCheckService : IHealthCheckService, IDisposable
    {
        private readonly ConcurrentDictionary<Tuple<string, int>, MonitorEntry> _dictionaries = new ConcurrentDictionary<Tuple<string, int>, MonitorEntry>();

        private readonly ConcurrentDictionary<Tuple<string, int,string>, MonitorEntry> _timeoutDictionaries = new ConcurrentDictionary<Tuple<string, int,string>, MonitorEntry>();

        private readonly IServiceRouteManager _serviceRouteManager;
        private readonly int _timeout = AppConfig.ServerOptions.HealthCheckTimeout;
        private readonly Timer _timer;
        private EventHandler<HealthCheckEventArgs> _removed;

        private EventHandler<HealthCheckEventArgs> _changed;
        private readonly ILogger<DefaultHealthCheckService> _logger;
        public event EventHandler<HealthCheckEventArgs> Removed
        {
            add { _removed += value; }
            remove { _removed -= value; }
        }

        public event EventHandler<HealthCheckEventArgs> Changed
        {
            add { _changed += value; }
            remove { _changed -= value; }
        }

        /// <summary>
        /// 默认心跳检查服务(每10秒会检查一次服务状态，在构造函数中添加服务管理事件) 
        /// </summary>
        /// <param name="serviceRouteManager"></param>
        public DefaultHealthCheckService(IServiceRouteManager serviceRouteManager)
        {
            _logger = ServiceLocator.GetService<ILogger<DefaultHealthCheckService>>();
            var timeSpan = TimeSpan.FromSeconds(AppConfig.ServerOptions.HealthCheckWatchIntervalInSeconds);

            _serviceRouteManager = serviceRouteManager;
            //建立计时器
            _timer = new Timer(async s =>
            {
                //检查服务是否可用
                await Check(_dictionaries.ToArray().Select(i => i.Value), _timeout);
                //移除不可用的服务地址
                ///RemoveUnhealthyAddress(_dictionaries.ToArray().Select(i => i.Value).Where(m => m.UnhealthyTimes >= AppConfig.ServerOptions.AllowServerUnhealthyTimes));
            }, null, timeSpan, timeSpan);
            
            //去除监控。
            serviceRouteManager.Removed += (s, e) =>
            {
                Remove(e.Route.Address);
            };
            //重新监控。
            serviceRouteManager.Created += async (s, e) =>
            {
                var keys = e.Route.Address.Select(address =>
                {
                    var ipAddress = address as IpAddressModel;
                    return new Tuple<string, int>(ipAddress.Ip, ipAddress.Port);
                });
                await Check(_dictionaries.Where(i => keys.Contains(i.Key)).Select(i => i.Value), _timeout);

            };
            //重新监控。
            serviceRouteManager.Changed += async (s, e) =>
            {
                var keys = e.Route.Address.Select(address => {
                    var ipAddress = address as IpAddressModel;
                    return new Tuple<string, int>(ipAddress.Ip, ipAddress.Port);
                });
                await Check(_dictionaries.Where(i => keys.Contains(i.Key)).Select(i => i.Value), _timeout);
            };
        }


        #region Implementation of IHealthCheckService

        /// <summary>
        /// 监控一个地址。
        /// </summary>
        /// <param name="address">地址模型。</param>
        /// <returns>一个任务。</returns>
        public async Task Monitor(AddressModel address)
        {
            var ipAddress = address as IpAddressModel;
            if (!_dictionaries.TryGetValue(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), out MonitorEntry monitorEntry)) 
            {
                monitorEntry = new MonitorEntry(ipAddress);
                await Check(monitorEntry,_timeout);
                _dictionaries.TryAdd(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), monitorEntry);
            }
            OnChanged(new HealthCheckEventArgs(address, monitorEntry.Health));
        }

        /// <summary>
        /// 判断一个地址是否健康。
        /// </summary>
        /// <param name="address">地址模型。</param>
        /// <returns>健康返回true，否则返回false。</returns>
        public async Task<bool> IsHealth(AddressModel address)
        {
            var ipAddress = address as IpAddressModel;
            MonitorEntry entry;
            if (!_dictionaries.TryGetValue(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), out entry))
            {
                entry = new MonitorEntry(address);
                await Check(entry, _timeout);
                _dictionaries.TryAdd(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), entry);
            }

            if (entry.UnhealthyTimes >= AppConfig.ServerOptions.AllowServerUnhealthyTimes)
            {
               await RemoveUnhealthyAddress(entry);
            }
            OnChanged(new HealthCheckEventArgs(address, entry.Health));
            return entry.Health;

        }


        /// <summary>
        /// 标记一个地址为失败的。
        /// </summary>
        /// <param name="address">地址模型。</param>
        /// <returns>一个任务。</returns>
        public Task MarkFailure(AddressModel address)
        {
            return Task.Run(() =>
            {
                var ipAddress = address as IpAddressModel;
                var entry = _dictionaries.GetOrAdd(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), k => new MonitorEntry(address));
                entry.Health = false;
                entry.UnhealthyTimes += 1;
            });
        }
        public Task MarkSuccess(AddressModel address,string serviceId) 
        {
            return Task.Run(() =>
            {
                var ipAddress = address as IpAddressModel;
                var entry = _timeoutDictionaries.TryRemove(new Tuple<string, int,string>(ipAddress.Ip, ipAddress.Port, serviceId), out MonitorEntry value);             
            });
        }

        public Task MarkTimeout(AddressModel address, string serviceId)
        {
            return Task.Run(() =>
            {
                var ipAddress = address as IpAddressModel;
                var entry = _timeoutDictionaries.GetOrAdd(new Tuple<string, int, string>(ipAddress.Ip, ipAddress.Port, serviceId), k => new MonitorEntry(address));
                if (entry.TimeOutTimes >= AppConfig.ServerOptions.AllowServerTimeOutTimes)
                {
                    RemoveTimeoutAddress(entry, serviceId);
                }
                else 
                {
                    entry.TimeOutTimes += 1;
                }
               
            });
        }

        protected void OnRemoved(params HealthCheckEventArgs[] args)
        {
            if (_removed == null)
                return;

            foreach (var arg in args)
                _removed(this, arg);
        }

        protected void OnChanged(params HealthCheckEventArgs[] args)
        {
            if (_changed == null)
                return;

            foreach (var arg in args)
                _changed(this, arg);
        }

        #endregion Implementation of IHealthCheckService

        #region Implementation of IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _timer.Dispose();
        }

        #endregion Implementation of IDisposable

        #region Private Method

        private void Remove(IEnumerable<AddressModel> addressModels)
        {
            foreach (var addressModel in addressModels)
            {
                MonitorEntry value;
                var ipAddress = addressModel as IpAddressModel;
                _dictionaries.TryRemove(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), out value);
            }
        }

        private async Task RemoveUnhealthyAddress(IEnumerable<MonitorEntry> monitorEntry)
        {
            if (monitorEntry.Any())
            {
                var addresses = monitorEntry.Select(p =>
                {
                    var ipAddress = p.Address as IpAddressModel;
                    return new IpAddressModel(ipAddress.Ip, ipAddress.Port);
                }).ToList();
                if (addresses.Any()) 
                {
                    await _serviceRouteManager.RemveAddressAsync(addresses);
                    addresses.ForEach(p => {
                        var ipAddress = p as IpAddressModel;
                        _dictionaries.TryRemove(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), out MonitorEntry value);
                    });
                    OnRemoved(addresses.Select(p => new HealthCheckEventArgs(p)).ToArray());
                }
                
            }
        }

        private void RemoveTimeoutAddress(MonitorEntry monitorEntry, string serviceId) 
        {
            var ipAddress = monitorEntry.Address as IpAddressModel;
            var address = new IpAddressModel(ipAddress.Ip, ipAddress.Port);
            _serviceRouteManager.RemveAddressAsync(new List<AddressModel>() { address }, serviceId).Wait();
        }


        private async Task RemoveUnhealthyAddress(MonitorEntry monitorEntry)
        {
            var ipAddress = monitorEntry.Address as IpAddressModel;            
            await _serviceRouteManager.RemveAddressAsync(new List<AddressModel>() { ipAddress });
            _dictionaries.TryRemove(new Tuple<string, int>(ipAddress.Ip, ipAddress.Port), out MonitorEntry value);
            OnRemoved(new HealthCheckEventArgs(ipAddress));
        }

        private async Task Check(MonitorEntry entry, int timeout)
        {
            var ipAddress = entry.Address as IpAddressModel;
            if (SocketCheck.TestConnection(ipAddress.Ip, ipAddress.Port, timeout))
            {
                entry.UnhealthyTimes = 0;
                entry.Health = true;
            }
            else
            {
                if (entry.UnhealthyTimes >= AppConfig.ServerOptions.AllowServerUnhealthyTimes)
                {
                    _logger.LogWarning($"服务地址{entry.Address}不健康,UnhealthyTimes={entry.UnhealthyTimes},服务将会被移除");
                    await RemoveUnhealthyAddress(entry);
                }
                else 
                {
                    entry.UnhealthyTimes++;
                    entry.Health = false;
                    _logger.LogWarning($"服务地址{entry.Address}不健康,UnhealthyTimes={entry.UnhealthyTimes}");
                }
            }


        }

        private async Task Check(IEnumerable<MonitorEntry> entrys, int timeout)
        {
            foreach (var entry in entrys)
            {
                var ipAddress = entry.Address as IpAddressModel;
                if (SocketCheck.TestConnection(ipAddress.Ip, ipAddress.Port, timeout))
                {
                    entry.UnhealthyTimes = 0;
                    entry.Health = true;
                }
                else
                {
                    if (entry.UnhealthyTimes >= AppConfig.ServerOptions.AllowServerUnhealthyTimes)
                    {
                        _logger.LogWarning($"服务地址{entry.Address}不健康,UnhealthyTimes={entry.UnhealthyTimes},服务将会被移除");
                        await RemoveUnhealthyAddress(entry);
                    }
                    else
                    {
                        entry.UnhealthyTimes++;
                        entry.Health = false;
                        _logger.LogWarning($"服务地址{entry.Address}不健康,UnhealthyTimes={entry.UnhealthyTimes}");
                    }
                }

            }
        }

        #endregion Private Method

        #region Help Class

        protected class MonitorEntry
        {
            public MonitorEntry(AddressModel addressModel)
            {
                Address = addressModel;
                Health = false;
                UnhealthyTimes = 0;

            }

            public int UnhealthyTimes { get; set; }

            public int TimeOutTimes { get; set; }

            public AddressModel Address { get; set; }

            public bool Health { get; set; }
        }

        #endregion Help Class
    }
}