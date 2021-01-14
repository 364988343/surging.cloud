using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Surging.Cloud.CPlatform.Address;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.CPlatform.Runtime.Client;
using Surging.Cloud.CPlatform.Runtime.Server;
using Surging.Cloud.CPlatform.Utilities;

namespace Surging.Cloud.CPlatform
{
    public class ClientHostedService : IHostedService
    {

        private readonly IServiceEntryManager _serviceEntryManager;
        private readonly IServiceSubscribeManager _serviceSubscribeManager;
        private readonly IModuleProvider _moduleProvider;

        public ClientHostedService(IServiceEntryManager serviceEntryManager,
            IServiceSubscribeManager serviceSubscribeManager,
            IServiceProvider serviceProvider,
            IModuleProvider moduleProvider)
        {
            _serviceEntryManager = serviceEntryManager;
            _serviceSubscribeManager = serviceSubscribeManager;
            _moduleProvider = moduleProvider;
            ServiceLocator.Current ??= serviceProvider.GetAutofacRoot();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var addressDescriptors = _serviceEntryManager.GetEntries().Select(i =>
            {
                i.Descriptor.Metadatas = null;
                return new ServiceSubscriber
                {
                    Address = new[] { new IpAddressModel {
                        Ip = Dns.GetHostEntry(Dns.GetHostName())
                            .AddressList.FirstOrDefault<IPAddress>
                                (a => a.AddressFamily.ToString().Equals("InterNetwork"))
                            ?.ToString() } },
                    ServiceDescriptor = i.Descriptor
                };
            }).ToList();
            await _serviceSubscribeManager.SetSubscribersAsync(addressDescriptors);
            _moduleProvider.Initialize();
        }
        

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}