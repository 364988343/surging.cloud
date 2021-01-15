using Surging.Cloud.CPlatform;
using Surging.Cloud.ProxyGenerator.Implementation;
using Autofac;
using System;
using Surging.Cloud.ProxyGenerator.Interceptors;
using Surging.Cloud.ProxyGenerator.Interceptors.Implementation; 
using Surging.Cloud.CPlatform.Runtime.Client;
using Surging.Cloud.CPlatform.Convertibles; 

namespace Surging.Cloud.ProxyGenerator
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// 添加客户端代理
        /// </summary>
        /// <param name="builder">服务构建者</param>
        /// <returns>服务构建者。</returns>
        public static IServiceBuilder AddClientProxy(this IServiceBuilder builder)
        {
            var services = builder.Services;
            services.RegisterType<ServiceProxyGenerater>().As<IServiceProxyGenerater>().SingleInstance();
            services.RegisterType<ServiceProxyProvider>().As<IServiceProxyProvider>().SingleInstance();
            builder.Services.Register(provider =>new ServiceProxyFactory(
                 provider.Resolve<IRemoteInvokeService>(),
                 provider.Resolve<ITypeConvertibleService>(),
                 provider.Resolve<IServiceProvider>(),
                 builder.GetInterfaceService(),
                 builder.GetDataContractName()
                 )).As<IServiceProxyFactory>().SingleInstance();
            return builder;
        }

        public static IServiceBuilder AddClientIntercepted(this IServiceBuilder builder,params Type[] interceptorServiceTypes )
        {
            var services = builder.Services; 
            services.RegisterTypes(interceptorServiceTypes).As<IInterceptor>().SingleInstance();
            services.RegisterType<InterceptorProvider>().As<IInterceptorProvider>().SingleInstance();
            return builder;
        }
        
        
    }
}