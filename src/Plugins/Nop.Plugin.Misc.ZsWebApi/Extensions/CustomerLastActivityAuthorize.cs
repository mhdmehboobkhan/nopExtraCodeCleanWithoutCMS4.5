using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Extensions
{
    public class CustomerLastActivityAuthorizeAttribute : TypeFilterAttribute
    {
        #region ctor
        
        public CustomerLastActivityAuthorizeAttribute() : base(typeof(CustomerLastActivityAuthorize))
        {
        }
        
        #endregion

        public class CustomerLastActivityAuthorize : IAuthorizationFilter
        {
            public async Task OnActionExecutingAsync(AuthorizationFilterContext filterContext)
            {
                if (!DataSettingsManager.IsDatabaseInstalled())
                    return;

                if (filterContext?.HttpContext.Request == null)
                    return;

                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var customer = await workContext.GetCurrentCustomerAsync();

                //update last activity date
                if (customer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
                {
                    var customerService = EngineContext.Current.Resolve<ICustomerService>();
                    customer.LastActivityDateUtc = DateTime.UtcNow;
                    await customerService.UpdateCustomerAsync(customer);
                }
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                //do nothing
            }
        }
    }
}