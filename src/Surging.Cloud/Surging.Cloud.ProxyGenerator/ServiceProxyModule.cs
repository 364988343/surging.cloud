using Autofac;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Engines;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.CPlatform.Routing;
using System.Collections.Generic;
using Surging.Cloud.CPlatform.Runtime.Server;
using Surging.Cloud.ProxyGenerator.Diagnostics;
using Surging.Cloud.CPlatform.Diagnostics;

namespace Surging.Cloud.ProxyGenerator
{
   public class ServiceProxyModule: EnginePartModule
    {
        public override void Initialize(AppModuleContext context)
        {
            var serviceProvider = context.ServiceProvoider;
             serviceProvider.Resolve<IServiceProxyFactory>();
           
        }

        /// <summary>
        /// Inject dependent third-party components
        /// </summary>
        /// <param name="builder"></param>
        protected override void RegisterBuilder(ContainerBuilderWrapper builder)
        {
            base.RegisterBuilder(builder);
            builder.ContainerBuilder.AddCoreService().AddClientProxy();
            builder.RegisterType<RpcTransportDiagnosticProcessor>().As<ITracingDiagnosticProcessor>().SingleInstance();
        }
    }
}
