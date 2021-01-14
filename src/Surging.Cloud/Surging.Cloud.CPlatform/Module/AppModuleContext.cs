using Surging.Cloud.CPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Surging.Cloud.CPlatform.Module
{
   public class AppModuleContext
    {
        public AppModuleContext(List<AbstractModule> modules, 
            string[] virtualPaths,
            ILifetimeScope serviceProvoider)
        { 
            Modules = Check.NotNull(modules, nameof(modules));
            VirtualPaths = Check.NotNull(virtualPaths, nameof(virtualPaths));
            ServiceProvoider = Check.NotNull(serviceProvoider, nameof(serviceProvoider));
        }

        public List<AbstractModule> Modules { get; }

        public string[] VirtualPaths { get; }

        public ILifetimeScope ServiceProvoider { get; }
    }
}
