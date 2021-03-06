﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Surging.Cloud.ApiGateWay.Configurations;
using Surging.Cloud.CPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Surging.Cloud.ApiGateWay
{
    public static class AppConfig
    {
        public static IConfigurationRoot Configuration { get; set; }


        private static string _authorizationServiceKey;
        public static string AuthorizationServiceKey
        {
            get
            {
                if (Configuration == null)
                    return _authorizationServiceKey;
                return Configuration["AuthorizationServiceKey"] ?? _authorizationServiceKey;
            }
             set
            {

                _authorizationServiceKey = value;
            }
        }

        private static string _authorizationRoutePath;
        public static string AuthorizationRoutePath
        {
            get
            {
                if (Configuration == null)
                    return _authorizationRoutePath;
                return Configuration["AuthorizationRoutePath"] ?? _authorizationRoutePath;
            }
            set
            {

                _authorizationRoutePath = value;
            }
        }

        private static string _authenticationServiceKey;

        public static string AuthenticationServiceKey {
            get
            {
                if (Configuration == null)
                    return _authenticationServiceKey;
                return Configuration["AuthenticationServiceKey"] ?? _authenticationServiceKey;
            }
            set
            {

                _authenticationServiceKey = value;
            }
        }

        private static string _authenticationRoutePath;

        public static string AuthenticationRoutePath
        {
            get
            {
                if (Configuration == null)
                    return _authenticationRoutePath;
                return Configuration["AuthenticationRoutePath"] ?? _authenticationRoutePath;
            }
            set
            {

                _authenticationRoutePath = value;
            }
        }

        private static string _tokenEndpointPath = "oauth2/token";

        public static string  TokenEndpointPath
        {
            get
            {
                if (Configuration == null)
                    return _tokenEndpointPath;
                return Configuration["TokenEndpointPath"] ?? _tokenEndpointPath;
            }
            set
            {
                _tokenEndpointPath = value;
            }
        }

        public static Register Register
        {
            get
            {
                var result = new Register();
                var section= Configuration.GetSection("Register");
                if (section != null)
                    result=  section.Get<Register>();
                return result;
            }

        }

        public static ServicePart ServicePart
        {
            get
            {
                var result = new ServicePart();
                var section = Configuration.GetSection("ServicePart");
                if (section != null)
                    result = section.Get<ServicePart>();
                return result;
            }
        }

        public static AccessPolicy Policy
        {
            get
            {
                var result = new AccessPolicy();
                var section = Configuration.GetSection("AccessPolicy");
                if (section.Exists() )
                    result = section.Get<AccessPolicy>();
                return result;
            }
        }


        private static bool _isUsingTerminal = false;

        public static bool IsUsingTerminal { 
            get 
            {
                if (Configuration == null)
                    return _isUsingTerminal;
                if (Configuration["IsUsingTerminal"] != null)
                {
                    return Convert.ToBoolean(Configuration["IsUsingTerminal"]);
                }
                else 
                {
                    return _isUsingTerminal;
                }
               
            }
            set 
            {
                _isUsingTerminal = value;
            }
        }

        private static string _terminals = "Dashborad,App";

        public static string Terminals 
        { 
            get 
            {
                if (Configuration == null)
                    return string.Empty;
                return Configuration["Terminals"] ?? _terminals;


            }
            set
            {
                _terminals = value;
            }
        }

        private static string _tokenSecret;

        public static string JwtSecret
        {
            get
            {
                if (Configuration == null)
                    return _tokenSecret;
                return Configuration["TokenSecret"] ?? _tokenSecret;


            }
            set
            {
                _tokenSecret = value;
            }
        }
        private static int _defaultExpired = 72;
        public static int DefaultExpired {
            get
            {
                if (Configuration == null)
                    return _defaultExpired;
                return Configuration["DefaultExpired"]?.To<int>() ?? _defaultExpired;


            }
            set
            {
                _defaultExpired = value;
            }
        }
    }
}
