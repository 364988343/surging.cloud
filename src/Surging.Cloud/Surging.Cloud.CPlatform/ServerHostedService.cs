using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.CPlatform.Routing;
using Surging.Cloud.CPlatform.Runtime.Server;
using Surging.Cloud.CPlatform.Support;
using Surging.Cloud.CPlatform.Utilities;

namespace Surging.Cloud.CPlatform
{
    public class ServerHostedService : IHostedService
    {
        private readonly IServiceTokenGenerator _serviceTokenGenerator;
        private readonly IServiceCommandManager _serviceCommandManager;
        private readonly IServiceRouteProvider _serviceRouteProvider;
        private readonly IModuleProvider _moduleProvider;
        public ServerHostedService(IServiceTokenGenerator serviceTokenGenerator,
            IServiceCommandManager serviceCommandManager,
            IServiceRouteProvider serviceRouteProvider, 
            IServiceProvider serviceProvider, 
            IModuleProvider moduleProvider)
        {
            _serviceTokenGenerator = serviceTokenGenerator;
            _serviceCommandManager = serviceCommandManager;
            _serviceRouteProvider = serviceRouteProvider;
            _moduleProvider = moduleProvider;
            ServiceLocator.Current ??= serviceProvider.GetAutofacRoot();

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceTokenGenerator.GeneratorToken(AppConfig.ServerOptions.Token);
            int _port = AppConfig.ServerOptions.Port = AppConfig.ServerOptions.Port == 0 ? 100 : AppConfig.ServerOptions.Port;
            string _ip =  AppConfig.ServerOptions.Ip ?? "0.0.0.0";
            _port = AppConfig.ServerOptions.Port = AppConfig.ServerOptions.IpEndpoint?.Port ?? _port;
            _ip = AppConfig.ServerOptions.Ip = AppConfig.ServerOptions.IpEndpoint?.Address.ToString() ?? _ip;
            _ip = NetUtils.GetHostAddress(_ip);
            _moduleProvider.Initialize();
            if (!AppConfig.ServerOptions.DisableServiceRegistration)
            {
                await _serviceCommandManager.SetServiceCommandsAsync();
                if (AppConfig.ServerOptions.Protocol == CommunicationProtocol.Tcp ||
                    AppConfig.ServerOptions.Protocol == CommunicationProtocol.None)
                {
                    await _serviceRouteProvider.RegisterRoutes(Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds);

                } 
            }
            var serviceHosts = ServiceLocator.Current.Resolve<IList<Runtime.Server.IServiceHost>>();
            await Task.Factory.StartNew(async () =>
            {
                foreach (var serviceHost in serviceHosts)
                    await serviceHost.StartAsync(_ip, _port);
                
            }, cancellationToken);
        }
        

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }
    }
}