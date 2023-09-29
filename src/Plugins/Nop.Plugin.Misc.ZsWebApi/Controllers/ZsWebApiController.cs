using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Misc.ZsWebApi.Extensions;
using Nop.Plugin.Misc.ZsWebApi.Factories;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using Nop.Plugin.Misc.ZsWebApi.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.ZsWebApi.Controllers
{
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class ZsWebApiController : BasePluginController
    {
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        public ZsWebApiController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var zsWebApiSettings = await _settingService.LoadSettingAsync<ZsWebApiSettings>(storeScope);
            var model = new ConfigurationModel
            {
                Enable = zsWebApiSettings.Enable,
                NSTKey = zsWebApiSettings.NSTKey,
                NSTSecret = zsWebApiSettings.NSTSecret,
                NSTToken = zsWebApiSettings.NSTToken,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.Enable_OverrideForStore = await _settingService.SettingExistsAsync(zsWebApiSettings, x => x.Enable, storeScope);
                model.NSTKey_OverrideForStore = await _settingService.SettingExistsAsync(zsWebApiSettings, x => x.NSTKey, storeScope);
                model.NSTSecret_OverrideForStore = await _settingService.SettingExistsAsync(zsWebApiSettings, x => x.NSTSecret, storeScope);
            }

            return View("~/Plugins/Misc.ZsWebApi/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageWidgets))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var zsWebApiSettings = await _settingService.LoadSettingAsync<ZsWebApiSettings>(storeScope);

            zsWebApiSettings.Enable = model.Enable;
            
            if(model.NSTKey != zsWebApiSettings.NSTKey || model.NSTSecret != zsWebApiSettings.NSTSecret)
            {
                var payload = new Dictionary<string, object>
                {
                    { PluginDefaults.NST_KEY, model.NSTKey }
                };

                var token = JwtHelper.JwtEncoder.Encode(payload, model.NSTSecret);
                zsWebApiSettings.NSTToken = token;
            }

            zsWebApiSettings.NSTKey = model.NSTKey;
            zsWebApiSettings.NSTSecret = model.NSTSecret;


            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(zsWebApiSettings, x => x.Enable, model.Enable_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zsWebApiSettings, x => x.NSTKey, model.NSTKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zsWebApiSettings, x => x.NSTSecret, model.NSTSecret_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(zsWebApiSettings, x => x.NSTToken, (model.NSTKey_OverrideForStore || model.NSTSecret_OverrideForStore), storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));
            return await Configure();
        }
    }
}