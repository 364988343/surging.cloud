﻿using Autofac;
using Microsoft.AspNetCore.Http;
using Surging.Cloud.ApiGateWay;
using Surging.Cloud.ApiGateWay.OAuth;
using Surging.Cloud.CPlatform;
using Surging.Cloud.CPlatform.Exceptions;
using Surging.Cloud.CPlatform.Filters.Implementation;
using Surging.Cloud.CPlatform.Messages;
using Surging.Cloud.CPlatform.Transport.Implementation;
using Surging.Cloud.CPlatform.Utilities;
using Surging.Cloud.KestrelHttpServer.Filters;
using Surging.Cloud.KestrelHttpServer.Filters.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Surging.Cloud.Stage.Filters
{
    public class ActionFilterAttribute : IActionFilter
    {
        private readonly IAuthorizationServerProvider _authorizationServerProvider;
        private const int _order = 9997;

        public int Order => _order;

        public ActionFilterAttribute()
        {
            _authorizationServerProvider = ServiceLocator.Current.Resolve<IAuthorizationServerProvider>();
        }

        public Task OnActionExecuted(ActionExecutedContext filterContext)
        {
            return Task.CompletedTask;
        }

        public async Task OnActionExecuting(ActionExecutingContext filterContext)
        {
            var gatewayAppConfig = AppConfig.Options.ApiGetWay;
            if (filterContext.Message.RoutePath == gatewayAppConfig.AuthenticationRoutePath)
            {
                var terminal = filterContext.Context.Request.Headers["x-terminal"];
                if (gatewayAppConfig.IsUsingTerminal) 
                {                   
                    if (!terminal.Any()) 
                    {
                        filterContext.Result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.RequestError, Message = "请设置请求头x-terminal" };
                    }
                    if (gatewayAppConfig.Terminals.Split(",").Any(p => p == terminal)) 
                    {
                        filterContext.Result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.RequestError, Message = $"不支持名称为{terminal}的终端,请检查设置的请求头x-terminal" };
                    }
                    //filterContext.Message.Parameters.Add("terminal", terminal);
                    RpcContext.GetContext().SetAttachment("x-terminal", terminal.ToString());
                }
                var token = await _authorizationServerProvider.IssueToken(new Dictionary<string, object>(filterContext.Message.Parameters));
                if (token != null)
                {
                    filterContext.Result = HttpResultMessage<object>.Create(true, token);
                    filterContext.Result.StatusCode = StatusCode.Success;
                }
                else
                {
                    filterContext.Result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.RequestError, Message = "请求失败,请稍后重试" };
                }
            }
            else if (filterContext.Route.ServiceDescriptor.AuthType() == AuthorizationType.AppSecret.ToString())
            {
                if (!ValidateAppSecretAuthentication(filterContext, out HttpResultMessage<object> result))
                {
                    filterContext.Result = result;
                }
            }
        }

        private bool ValidateAppSecretAuthentication(ActionExecutingContext filterContext, out HttpResultMessage<object> result)
        {
            bool isSuccess = true;
            DateTime time;
            result = HttpResultMessage<object>.Create(true,null);
            
            if (!filterContext.Route.ServiceDescriptor.EnableAuthorization()) 
            {
                return isSuccess;
            }
            var author = filterContext.Context.Request.Headers["Authorization"];
            var model = filterContext.Message.Parameters;
            var route = filterContext.Route;
            if (model.ContainsKey("timeStamp") && author.Count > 0)
            {
                if (long.TryParse(model["timeStamp"].ToString(), out long timeStamp))
                {
                    time = DateTimeConverter.UnixTimestampToDateTime(timeStamp);
                    var seconds = (DateTime.Now - time).TotalSeconds;
                    if (seconds <= 3560 && seconds >= 0)
                    {
                        if (GetMD5($"{route.ServiceDescriptor.Token}{time.ToString("yyyy-MM-dd hh:mm:ss") }") != author.ToString())
                        {
                            result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.UnAuthentication, Message = "Invalid authentication credentials" };
                            isSuccess = false;
                        }
                    }
                    else
                    {
                        result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.UnAuthentication, Message = "Invalid authentication credentials" };
                        isSuccess = false;
                    }
                }
                else
                {
                    result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.UnAuthentication, Message = "Invalid authentication credentials" };
                    isSuccess = false;
                }
            }
            else
            {
                // todo 认证 AppAppSecret
                result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = StatusCode.RequestError, Message = "Request error" };

                isSuccess = false;
            }
            return isSuccess;
        }

        public  string GetMD5(string encypStr)
        {
            try
            {
                var md5 = MD5.Create();
                var bs = md5.ComputeHash(Encoding.UTF8.GetBytes(encypStr));
                var sb = new StringBuilder();
                foreach (byte b in bs)
                {
                    sb.Append(b.ToString("X2"));
                } 
                return sb.ToString().ToLower();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                return null;
            }
        }
    }
}
