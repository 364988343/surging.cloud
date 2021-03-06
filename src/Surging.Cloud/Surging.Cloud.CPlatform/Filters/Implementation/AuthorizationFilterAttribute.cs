﻿using Surging.Cloud.CPlatform.Routing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Surging.Cloud.CPlatform.Filters.Implementation
{
    public abstract class AuthorizationFilterAttribute : FilterAttribute,IAuthorizationFilter, IFilter
    {
        public virtual bool OnAuthorization(ServiceRouteContext context)
        {
            return true;
        }

        public virtual void ExecuteAuthorizationFilterAsync(ServiceRouteContext serviceRouteContext, CancellationToken cancellationToken)
        {
            var result = OnAuthorization(serviceRouteContext);
            if (!result)
            {
                serviceRouteContext.ResultMessage.StatusCode = Exceptions.StatusCode.UnAuthentication;
                serviceRouteContext.ResultMessage.ExceptionMessage = "令牌验证失败.";
            }
        }
    }
}
