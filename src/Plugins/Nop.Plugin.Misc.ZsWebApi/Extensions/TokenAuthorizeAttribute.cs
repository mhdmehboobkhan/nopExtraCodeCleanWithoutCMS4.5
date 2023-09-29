using JWT;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using Nop.Plugin.Misc.ZsWebApi.Models;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public class TokenAuthorizeAttribute : TypeFilterAttribute
    {
        #region Ctor

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public TokenAuthorizeAttribute() : base(typeof(TokenAuthorizeAttributeFilter))
        {
        }

        #endregion

        #region nested class

        public class TokenAuthorizeAttributeFilter : IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext actionContext)
            {

                var identity = ParseAuthorizationHeader(actionContext);
                if (identity == false)
                {
                    Challenge(actionContext);
                    return;
                }
            }

            protected virtual bool ParseAuthorizationHeader(AuthorizationFilterContext actionContext)
            {
                bool check = true;
                StringValues checkToken;
                if (actionContext.HttpContext.Request.Headers.TryGetValue(PluginDefaults.TokenName, out checkToken))
                {
                    var token = checkToken.FirstOrDefault();
                    var secretKey = PluginDefaults.SecretKey;
                    try
                    {
                        IDictionary<string, object> payload = JwtHelper.JwtDecoder.DecodeToObject(token, secretKey, true) as IDictionary<string, object>;
                        check = true;
                    }
                    catch
                    {
                        check = false;
                    }
                }

                return check;
            }

            void Challenge(AuthorizationFilterContext actionContext)
            {
                var host = actionContext.HttpContext.Request.Host;
                var response = new BaseResponse
                {
                    StatusCode = (int)ErrorType.AuthenticationError,
                    ErrorList = new List<string>
                    {
                        "Token Expired.Please Login Again"
                    }
                };
                actionContext.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(response));
                //actionContext.Request.CreateResponse(HttpStatusCode.Forbidden, response);
                actionContext.HttpContext.Response.Headers.Add("WWW-Authenticate", string.Format("Basic realm=\"{0}\"", host));
            }
        }

        #endregion
    }
}
