using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Security;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JWT;
using System.Threading.Tasks;
using Nop.Services.ScheduleTasks;

namespace Nop.Plugin.Misc.ZsWebApi.Infrastructure
{
    public class ApiWebWorkContext : WebWorkContext
    {
        private Customer _cachedCustomer;
        private Customer _originalCustomerIfImpersonated;
        private readonly CookieSettings _cookieSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerService _customerService;
        private readonly IUserAgentHelper _userAgentHelper;
        private readonly IAuthenticationService _authenticationService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILanguageService _languageService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IWebHelper _webHelper;
        private readonly LocalizationSettings _localizationSettings;

        public ApiWebWorkContext(CookieSettings cookieSettings,
            IHttpContextAccessor httpContextAccessor,
            ICustomerService customerService,
            IStoreContext storeContext,
            IAuthenticationService authenticationService,
            ILanguageService languageService,
            IGenericAttributeService genericAttributeService,
            LocalizationSettings localizationSettings,
            IUserAgentHelper userAgentHelper,
            IStoreMappingService storeMappingService,
            IGenericAttributeService genericAttributeService1,
            ILanguageService languageService1,
            IStoreContext storeContext1,
            IStoreMappingService storeMappingService1,
            IWebHelper webHelper,
            LocalizationSettings localizationSettings1
          )
            : base(cookieSettings,
                  authenticationService,
                  customerService,
                  genericAttributeService,
                  httpContextAccessor,
                  languageService,
                  storeContext,
                  storeMappingService,
                  userAgentHelper,
                  webHelper,
                  localizationSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _customerService = customerService;
            _userAgentHelper = userAgentHelper;
            _authenticationService = authenticationService;
            _genericAttributeService = genericAttributeService1;
            _languageService = languageService1;
            _storeContext = storeContext1;
            _storeMappingService = storeMappingService1;
            _webHelper = webHelper;
            _localizationSettings = localizationSettings1;
        }

        public async Task<Customer> GetCustomerFromToken()
        {
            try
            {
                int id = 0;
                var secretKey = PluginDefaults.SecretKey;

                _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(PluginDefaults.TokenName, out var keyFound);
                var token = keyFound.FirstOrDefault();
                var load = JwtHelper.JwtDecoder.DecodeToObject(token, secretKey, true) as IDictionary<string, object>;
                if (load != null)
                {
                    id = Convert.ToInt32(load[PluginDefaults.CustomerIdName]);
                    return await _customerService.GetCustomerByIdAsync(id);
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public override async Task<Customer> GetCurrentCustomerAsync()
        {
            if (_cachedCustomer != null)
                return _cachedCustomer;
            
            await SetCurrentCustomerAsync();

            return _cachedCustomer;
        }

        public override async System.Threading.Tasks.Task SetCurrentCustomerAsync(Customer customer = null)
        {
            if (customer == null)
            {
                //check whether request is made by a background (schedule) task
                if (_httpContextAccessor.HttpContext == null ||
                    _httpContextAccessor.HttpContext.Request.Path.Equals(new PathString($"/{NopTaskDefaults.ScheduleTaskPath}"), StringComparison.InvariantCultureIgnoreCase))
                {
                    //in this case return built-in customer record for background task
                    customer = await _customerService.GetOrCreateBackgroundTaskUserAsync();
                }

                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    //check whether request is made by a search engine, in this case return built-in customer record for search engines
                    if (_userAgentHelper.IsSearchEngine())
                        customer = await _customerService.GetOrCreateSearchEngineUserAsync();
                }

                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    //try to get registered user
                    customer = await _authenticationService.GetAuthenticatedCustomerAsync();
                }

                //load mobile customer
                if (_httpContextAccessor.HttpContext?.Request.Path.Value != null && _httpContextAccessor.HttpContext.Request.Path.Value.StartsWith("/api/"))
                {
                    //check whether request is made by a background task
                    //in this case return built-in customer record for background task
                    if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey(PluginDefaults.TokenName))
                    {
                        customer = await GetCustomerFromToken();
                        if (customer != null)
                        {
                            _cachedCustomer = customer;
                        }
                    }
                }

                if (customer != null && !customer.Deleted && customer.Active && !customer.RequireReLogin)
                {
                    //get impersonate user if required
                    var impersonatedCustomerId = await _genericAttributeService.GetAttributeAsync<int?>(customer, Core.Domain.Customers.NopCustomerDefaults.ImpersonatedCustomerIdAttribute);
                    if (impersonatedCustomerId.HasValue && impersonatedCustomerId.Value > 0)
                    {
                        var impersonatedCustomer = await _customerService.GetCustomerByIdAsync(impersonatedCustomerId.Value);
                        if (impersonatedCustomer != null && !impersonatedCustomer.Deleted && impersonatedCustomer.Active && !impersonatedCustomer.RequireReLogin)
                        {
                            //set impersonated customer
                            _originalCustomerIfImpersonated = customer;
                            customer = impersonatedCustomer;
                        }
                    }
                }

                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    //get guest customer
                    var customerCookie = GetCustomerCookie();
                    if (!string.IsNullOrEmpty(customerCookie))
                    {
                        if (Guid.TryParse(customerCookie, out Guid customerGuid))
                        {
                            //get customer from cookie (should not be registered)
                            var customerByCookie = await _customerService.GetCustomerByGuidAsync(customerGuid);
                            if (customerByCookie != null && !await _customerService.IsRegisteredAsync(customerByCookie))
                                customer = customerByCookie;
                        }
                    }
                }

                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    //create guest if not exists
                    customer = await _customerService.InsertGuestCustomerAsync();
                }
            }

            if (!customer.Deleted && customer.Active && !customer.RequireReLogin)
            {
                //set customer cookie
                SetCustomerCookie(customer.CustomerGuid);

                //cache the found customer
                _cachedCustomer = customer;
            }
        }

        public override Customer OriginalCustomerIfImpersonated
        {
            get
            {
                return _originalCustomerIfImpersonated;
            }
        }
    }
}
