using System.Threading.Tasks;
using Surging.Cloud.CPlatform;
//using Surging.Cloud.EventBusKafka;
//using Surging.Cloud.Zookeeper;
//using Surging.Cloud.Zookeeper.Configurations;
using Microsoft.Extensions.Hosting;
using Surging.Cloud.Caching.Configurations;
using Surging.Cloud.CPlatform.Configurations;
using Surging.Cloud.CPlatform.Utilities;
using Surging.Cloud.Zookeeper;
using Surging.Cloud.Zookeeper.Configurations;

namespace Surging.Services.Server
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            //  Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//             var host = new ServiceHostBuilder()
//                 .RegisterServices(builder =>
//                 {
//                     builder.AddMicroService(option =>
//                     {
//                         option.AddServiceRuntime()
//                         .AddRelateService()
//                         .AddConfigurationWatch()
//                         //option.UseZooKeeperManager(new ConfigInfo("127.0.0.1:2181")); 
//                         .AddServiceEngine(typeof(SurgingServiceEngine))
//                         .AddClientIntercepted(typeof(CacheProviderInterceptor));
//                         builder.Register(p => new CPlatformContainer(ServiceLocator.Current));
//                     });
//                 })
//                 .ConfigureLogging(logger =>
//                 {
//                     logger.AddConfiguration(
//                         AppConfig.GetSection("Logging"));
//                 })
//                 .UseServer(options => { })
//                 .UseConsoleLifetime()
//                 .Configure(build =>
//                 {
//
// #if DEBUG
//                     build.AddCacheFile("${cachePath}|/app/configs/cacheSettings.json", optional: false, reloadOnChange: true);
//                     build.AddCPlatformFile("${surgingPath}|/app/configs/surgingSettings.json", optional: false, reloadOnChange: true);
//                     build.AddEventBusFile("${eventBusPath}|/app/configs/eventBusSettings.json", optional: false, reloadOnChange: true);
//                     build.AddConsulFile("${consulPath}|/app/configs/consul.json", optional: false, reloadOnChange: true);
//                     build.AddZookeeperFile("${zookeeperPath}|/app/configs/zookeeper.json", optional: false, reloadOnChange: true);
// #else
//                     build.AddCacheFile("${cachePath}|configs/cacheSettings.json", optional: false, reloadOnChange: true);
//                     build.AddCPlatformFile("${surgingPath}|configs/surgingSettings.json", optional: false, reloadOnChange: true);
//                     build.AddEventBusFile("${eventBusPath}|configs/eventBusSettings.json", optional: false);
//                     build.AddConsulFile("${consulPath}|configs/consul.json", optional: false, reloadOnChange: true);
//                     build.AddZookeeperFile("${zookeeperPath}|configs/zookeeper.json", optional: false, reloadOnChange: true);
// #endif
//                 })
//                 .UseStartup<Startup>()
//                 .Build();

            // using (host.Run())
            // {
            //     Console.WriteLine($"服务端启动成功，{DateTime.Now}。");
            // }

            await Host.CreateDefaultBuilder(args)
                .RegisterMicroServices()
                .ConfigureAppConfiguration((hostContext, configure) =>
                {
#if DEBUG
                    configure.AddCacheFile("${cachePath}|/app/configs/cacheSettings.json", optional: false, reloadOnChange: true);
                    configure.AddCPlatformFile("${surgingPath}|/app/configs/surgingSettings.json", optional: false,
                        reloadOnChange: true);
                    // configure.AddEventBusFile("${eventBusPath}|/app/configs/eventBusSettings.json", optional: false, reloadOnChange: true);
                    // configure.AddConsulFile("${consulPath}|/app/configs/consul.json", optional: false, reloadOnChange: true);
                    configure.AddZookeeperFile("${zookeeperPath}|/app/configs/zookeeper.json", optional: false,
                        reloadOnChange: true);
#else
                    configure.AddCacheFile("${cachePath}|configs/cacheSettings.json", optional: false, reloadOnChange: true);
                    configure.AddCPlatformFile("${surgingPath}|configs/surgingSettings.json", optional: false, reloadOnChange: true);
                    configure.AddEventBusFile("${eventBusPath}|configs/eventBusSettings.json", optional: false);
                    configure.AddConsulFile("${consulPath}|configs/consul.json", optional: false, reloadOnChange: true);
                    configure.AddZookeeperFile("${zookeeperPath}|configs/zookeeper.json", optional: false, reloadOnChange: true);
#endif
                })
                .UseServer()
                .UseClient()
                .RunConsoleAsync();


        }
    }
}
