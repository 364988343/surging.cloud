﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Surging.Cloud.CPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Surging.Cloud.CPlatform.Configurations
{
    public static class CacheConfigurationExtensionsstatic
    {
        public static IConfigurationBuilder AddCPlatformFile(this IConfigurationBuilder builder, string path)
        {
            return AddCPlatformFile(builder, provider: null, path: path,basePath: null, optional: false, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddCPlatformFile(this IConfigurationBuilder builder, string path, bool optional)
        {
            return AddCPlatformFile(builder, provider: null, path: path,basePath: null, optional: optional, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddCPlatformFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return AddCPlatformFile(builder, provider: null, path: path, basePath:null, optional: optional, reloadOnChange: reloadOnChange);
        }

        public static IConfigurationBuilder AddCPlatformFile(this IConfigurationBuilder builder, string path, string basePath, bool optional, bool reloadOnChange)
        {
            return AddCPlatformFile(builder, provider: null, path: path, basePath:basePath, optional: optional, reloadOnChange: reloadOnChange);
        }

        public static IConfigurationBuilder AddCPlatformFile(this IConfigurationBuilder builder, IFileProvider provider, string path,string basePath, bool optional, bool reloadOnChange)
        {
            Check.NotNull(builder, "builder");
            Check.CheckCondition(() => string.IsNullOrEmpty(path), "path");
            path = EnvironmentHelper.GetEnvironmentVariable(path);
            if (File.Exists(path))
            {
                if (provider == null && Path.IsPathRooted(path))
                {
                    provider = new PhysicalFileProvider(Path.GetDirectoryName(path));
                    path = Path.GetFileName(path);
                } 
                var source = new CPlatformConfigurationSource
                {
                    FileProvider = provider,
                    Path = path,
                    Optional = optional,
                    ReloadOnChange = reloadOnChange
                };
                
                builder.Add(source);
                if (!string.IsNullOrEmpty(basePath))
                    builder.SetBasePath(basePath);
                AppConfig.Configuration = builder.Build();
               
                var surgingSection = AppConfig.Configuration.GetSection("Surging");
                if (surgingSection.Exists())
                {
                    AppConfig.ServerOptions = surgingSection.Get<SurgingServerOptions>();
                }                
                var actionMapSecetion = AppConfig.Configuration.GetSection("Swagger:Options:MapRoutePaths");
                if (actionMapSecetion.Exists()) 
                {
                    AppConfig.MapRoutePathOptions = actionMapSecetion.Get<IEnumerable<MapRoutePathOption>>();
                }
            }
            return builder;
        }
    }
}