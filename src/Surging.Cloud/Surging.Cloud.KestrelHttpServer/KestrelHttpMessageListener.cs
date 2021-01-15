using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.CPlatform.Serialization;
using Surging.Cloud.KestrelHttpServer.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Surging.Cloud.CPlatform.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Surging.Cloud.KestrelHttpServer.Filters;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Surging.Cloud.CPlatform.Diagnostics;

namespace Surging.Cloud.KestrelHttpServer
{
    public class KestrelHttpMessageListener : HttpMessageListener, IDisposable
    {
        private readonly ILogger<KestrelHttpMessageListener> _logger;
        private IWebHost _host;
        private bool _isCompleted;
        private readonly ISerializer<string> _serializer;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IModuleProvider _moduleProvider;
        private readonly IServiceRouteProvider _serviceRouteProvider;
        private readonly ContainerBuilder _containerBuilder;
        public KestrelHttpMessageListener(ILogger<KestrelHttpMessageListener> logger,
            ISerializer<string> serializer,
            IHostApplicationLifetime hostApplicationLifetime,
            IModuleProvider moduleProvider,
            IServiceRouteProvider serviceRouteProvider,
            ContainerBuilder containerBuilder) : base(logger, serializer, serviceRouteProvider)
        {
            _logger = logger;
            _serializer = serializer;
            _moduleProvider = moduleProvider;
            _containerBuilder = containerBuilder;
            _serviceRouteProvider = serviceRouteProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public async Task StartAsync(IPAddress address,int? port)
        { 
            try
            {
                var hostBuilder = new WebHostBuilder()
                  .UseContentRoot(Directory.GetCurrentDirectory())
                  .UseKestrel((context,options) =>
                  {
                      // options.Limits.MinRequestBodyDataRate = null;
                      // options.Limits.MinResponseDataRate = null;
                      // options.Limits.MaxRequestBodySize = null;
                      // options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
                      if (port != null && port > 0)
                      {
                          options.Listen(address, port.Value, listenOptions =>
                          {
                              listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                          });
                      }
                      ConfigureHost(context, options, address);

                  })
                  .ConfigureServices(ConfigureServices)
                  .ConfigureLogging((logger) =>
                  {
                      logger.AddConfiguration(
                             CPlatform.AppConfig.GetSection("Logging"));
                  })
                  .Configure(AppResolve);

                if (Directory.Exists(CPlatform.AppConfig.ServerOptions.WebRootPath))
                    hostBuilder = hostBuilder.UseWebRoot(CPlatform.AppConfig.ServerOptions.WebRootPath);
                _host = hostBuilder.Build();
                _hostApplicationLifetime.ApplicationStarted.Register(async () =>
                {
                    if (_moduleProvider.Modules.Any(p=> p.ModuleName == "SwaggerModule" && p.Enable))
                    {
                        var httpProtocol = GetHttpProtocol();
                        _logger.LogInformation($"Kestrel主机将启动,Swagger文档地址为:{httpProtocol}://{address}:{port}/swagger/index.html");
                    }
                    else
                    {
                        _logger.LogInformation($"Kestrel主机即将启动");
                    }

                    await _host.RunAsync();
                });

            }
            catch(Exception ex)
            {
                _logger.LogError($"Kestrel服务主机启动失败，监听地址：{address}:{port}. ");
                throw ex;
            }

        }

        private string GetHttpProtocol()
        {
            var httpProtocol = "http";
            if (_moduleProvider.Modules.Any(p => p.ModuleName == "StageModule"))
            {
                var stageModule = _moduleProvider.Modules.First(p => p.ModuleName == "StageModule" && p.Enable);
                var enableHttpsObj = stageModule.GetType().GetProperty("EnableHttps").GetValue(stageModule);
                if (enableHttpsObj != null)
                {
                    httpProtocol = Convert.ToBoolean(enableHttpsObj) ? "https" : "http";
                }
            }

            return httpProtocol;
        }

        public void ConfigureHost(WebHostBuilderContext context, KestrelServerOptions options,IPAddress ipAddress)
        {
            _moduleProvider.ConfigureHost(new WebHostContext(context, options, ipAddress));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            _moduleProvider.ConfigureServices(new ConfigurationContext(services,
                _moduleProvider.Modules,
                _moduleProvider.VirtualPaths,
                AppConfig.Configuration));
            _containerBuilder.Populate(services); 
           
        }

        private void AppResolve(IApplicationBuilder app)
        { 
            app.UseStaticFiles();
            app.UseMvc();
            _moduleProvider.Initialize(new ApplicationInitializationContext(app, _moduleProvider.Modules,
                _moduleProvider.VirtualPaths,
                AppConfig.Configuration));
            app.Run(async (context) =>
            {
                var messageId = Guid.NewGuid().ToString("N");
                var sender = new HttpServerMessageSender(_serializer, context);
                // try
                // {
                //     var filters = app.ApplicationServices.GetServices<IAuthorizationFilter>().OrderByDescending(p=>p.Order);
                //     var isSuccess = await OnAuthorization(context, sender, messageId, filters);
                //     if (isSuccess)
                //     {
                //         var actionFilters = app.ApplicationServices.GetServices<IActionFilter>().OrderByDescending(p => p.Order);
                //         await OnReceived(sender, messageId, context, actionFilters);
                //     }
                // }
                // catch (Exception ex)
                // {
                //     var filters = app.ApplicationServices.GetServices<IExceptionFilter>();
                //     WirteDiagnosticError(messageId, ex);
                //     await OnException(context, sender, messageId, ex, filters);
                // }
                try
                {
                    await context.Response.WriteAsync("ok");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }

        private void WirteDiagnosticError(string messageId,Exception ex)
        {
            var diagnosticListener = new DiagnosticListener(DiagnosticListenerExtensions.DiagnosticListenerName);
            diagnosticListener.WriteTransportError(CPlatform.Diagnostics.TransportType.Rest, new TransportErrorEventData(new DiagnosticMessage
            {
                Id = messageId
            }, ex));
        }

        public void Dispose()
        {
            _host.Dispose();
        }

    }
}
