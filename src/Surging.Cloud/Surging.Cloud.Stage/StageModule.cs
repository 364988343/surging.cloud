using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Surging.Cloud.CPlatform.Module;
using Surging.Cloud.KestrelHttpServer;
using Surging.Cloud.KestrelHttpServer.Extensions;
using Surging.Cloud.KestrelHttpServer.Filters;
using Surging.Cloud.Stage.Configurations;
using Surging.Cloud.Stage.Filters;
using Surging.Cloud.Stage.Internal;
using Surging.Cloud.Stage.Internal.Implementation;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Autofac;

namespace Surging.Cloud.Stage
{
    public class StageModule : KestrelHttpModule
    {
        private IWebServerListener _listener;
        public override void Initialize(AppModuleContext context)
        {
            _listener = context.ServiceProvoider.Resolve<IWebServerListener>();
        }

        public override void RegisterBuilder(WebHostContext context)
        {
            EnableHttps = context.EnableHttps;
            _listener.Listen(context);
        }

        public override void Initialize(ApplicationInitializationContext context)
        {
            var policy = AppConfig.Options.AccessPolicy;
            if (policy != null)
            {
                context.Builder.UseCors(builder =>
                {
                    builder.WithOrigins(policy.Origins);
                    if (policy.AllowAnyHeader)
                        builder.AllowAnyHeader();
                    if (policy.AllowAnyMethod)
                        builder.AllowAnyMethod();
                    if (policy.AllowAnyOrigin)
                        builder.AllowAnyOrigin();
                    if (policy.AllowCredentials)
                        builder.AllowCredentials();
                });
            }
        }

        public override void RegisterBuilder(ConfigurationContext context)
        {
            var apiConfig = AppConfig.Options.ApiGetWay;
            if (apiConfig != null)
            {
                ApiGateWay.AppConfig.AuthenticationServiceKey = apiConfig.AuthenticationServiceKey;
                ApiGateWay.AppConfig.AuthorizationServiceKey = apiConfig.AuthorizationServiceKey;
                ApiGateWay.AppConfig.AuthorizationRoutePath = apiConfig.AuthorizationRoutePath;
                ApiGateWay.AppConfig.AuthenticationRoutePath = apiConfig.AuthenticationRoutePath;
                ApiGateWay.AppConfig.TokenEndpointPath = apiConfig.TokenEndpointPath;
                ApiGateWay.AppConfig.IsUsingTerminal = apiConfig.IsUsingTerminal;
                ApiGateWay.AppConfig.Terminals = apiConfig.Terminals;
                ApiGateWay.AppConfig.JwtSecret = apiConfig.JwtSecret;
                ApiGateWay.AppConfig.DefaultExpired = apiConfig.DefaultExpired;
            }
            context.Services.AddMvc().AddJsonOptions(options => {
                options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                if (AppConfig.Options.IsCamelCaseResolver)
                {
                    JsonConvert.DefaultSettings= new Func<JsonSerializerSettings>(() =>
                    {
                       JsonSerializerSettings setting = new Newtonsoft.Json.JsonSerializerSettings();
                        setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                        setting.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        return setting;
                    });
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }
                else
                {
                    JsonConvert.DefaultSettings = new Func<JsonSerializerSettings>(() =>
                    {
                        JsonSerializerSettings setting = new JsonSerializerSettings();
                        setting.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                        setting.ContractResolver= new DefaultContractResolver();
                        return setting;
                    });
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
            });
            context.Services.AddSingleton<IHttpContextAccessor,HttpContextAccessor>();
            context.Services.AddSingleton<IIPChecker,IPAddressChecker>();
            context.Services.AddFilters(typeof(AuthorizationFilterAttribute));
            context.Services.AddFilters(typeof(ActionFilterAttribute));
            context.Services.AddFilters(typeof(IPFilterAttribute));
        }

        protected override void RegisterBuilder(ContainerBuilderWrapper builder)
        {
            var section = CPlatform.AppConfig.GetSection("Stage");
            if (section.Exists())
            {
                AppConfig.Options = section.Get<StageOption>();
            }
            
            builder.RegisterType<WebServerListener>().As<IWebServerListener>().SingleInstance(); 
        }

        public bool EnableHttps { get; private set; }
    }
}
