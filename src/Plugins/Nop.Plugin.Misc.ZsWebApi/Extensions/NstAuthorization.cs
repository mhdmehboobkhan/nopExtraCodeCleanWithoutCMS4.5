using System.Collections.Generic;
using System.Linq;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using Nop.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using System;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using System.Threading.Tasks;
using Nop.Plugin.Misc.ZsWebApi.Models;
using JWT;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public class NstAuthorizationAttribute : TypeFilterAttribute
    {
        #region Ctor
        public NstAuthorizationAttribute() : base(typeof(NstAuthorization))
        {

        }

        #endregion

        #region Nested filter
        public class NstAuthorization : IActionFilter
        {
            public void OnActionExecuting(ActionExecutingContext filterContext)
            {
                var identity = ParseNstAuthorizationHeader(filterContext).Result;
                if (identity == false)
                {
                    CreateNstAccessResponceMessage(filterContext);
                    return;
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
                //do nothing
            }

            protected virtual async Task<bool> ParseNstAuthorizationHeader(ActionExecutingContext actionContext)
            {
                var httpContext = EngineContext.Current.Resolve<IHttpContextAccessor>().HttpContext;
                var settingService = EngineContext.Current.Resolve<ISettingService>();

                try
                {
                    var zsSettings = await settingService.LoadSettingAsync<ZsWebApiSettings>();
                    if(!zsSettings.Enable)
                        return false;

                    StringValues keyFound;
                    httpContext.Request.Headers.TryGetValue(PluginDefaults.NST, out keyFound);
                    var requestkey = keyFound.FirstOrDefault();
                    var load = JwtHelper.JwtDecoder.DecodeToObject(requestkey, zsSettings.NSTSecret, true) as IDictionary<string, object>;
                    if (load != null)
                    {
                        var key = load.FirstOrDefault();
                        return load[PluginDefaults.NST_KEY].ToString() == zsSettings.NSTKey;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
                return false;
            }

            void CreateNstAccessResponceMessage(ActionExecutingContext actionContext)
            {
                // var host = actionContext.Request.RequestUri.DnsSafeHost;
                var host = actionContext.HttpContext.Request.Host;
                var response = new BaseResponse
                {
                    StatusCode = (int)ErrorType.AuthenticationError,
                    ErrorList = new List<string>
                    {
                        "Nst Token Not Valid or api is not enable"
                    }
                };
                actionContext.Result = new BadRequestObjectResult(response);
                return;
            }
        }


        #endregion
    }
}
