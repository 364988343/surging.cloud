using Surging.Cloud.CPlatform.Support;
using Surging.Cloud.CPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Surging.Cloud.CPlatform.Configurations
{
    public  partial class SurgingServerOptions: ServiceCommand
    {
        public string Ip { get; set; }

        public string MappingIP { get; set; }

        public int MappingPort { get; set; }

        public string WanIp { get; set; }

        public bool IsModulePerLifetimeScope { get; set; }

        public int WatchInterval { get; set; } = 20;
        
        public int HealthCheckWatchIntervalInSeconds { get; set; } = 15;

        public bool EnableHealthCheck { get; set; } = true;

        public int AllowServerUnhealthyTimes { get; set; } = 5;

        public bool Libuv { get; set; } = false;

        public int SoBacklog { get; set; } = 8192;

        public IPEndPoint IpEndpoint { get; set; }

        public List<ModulePackage> Packages { get; set; } = new List<ModulePackage>();

        public CommunicationProtocol Protocol { get; set; }
        public string RootPath { get; set; }

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public int Port { get; set; }

        public bool DisableServiceRegistration { get; set; }

        public bool DisableDiagnostic { get; set; }
        
        public ProtocolPortOptions Ports { get; set; } = new  ProtocolPortOptions();

        public string Token { get; set; }

        public string NotRelatedAssemblyFiles { get; set; }

        public string RelatedAssemblyFiles { get; set; } = "";

        public RuntimeEnvironment Environment { get; set; } = RuntimeEnvironment.Production;

        public bool ForceDisplayStackTrace { get; set; }

        public int RpcConnectTimeout { get; set; } = 500;

        private string _hostName;
        public string HostName {
            get 
            {
                if (_hostName.IsNullOrEmpty()) 
                {
                    var hostAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(p => p.GetName().Name.ToLower().Contains("host") || p.GetName().Name.ToLower().Contains("server"));
                    if (hostAssembly != null) 
                    {
                        return string.Join(".", hostAssembly.GetName().Name.Split(".").Take(ProjectSegment));
                    }
                    return "";
                  
                }
                return _hostName;

            }
            set { _hostName = value; }
        }
        
        public int ProjectSegment { get; set; } = 3;
       
    }
}
