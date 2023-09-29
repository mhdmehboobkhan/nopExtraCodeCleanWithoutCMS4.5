using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Common;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using Nop.Web.Framework.Components;

namespace Nop.Web.Areas.Admin.Components
{
    public class MultistoreDisabledWarningViewComponent : NopViewComponent
    {
        private readonly CommonSettings _commonSettings;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;

        public MultistoreDisabledWarningViewComponent(CommonSettings commonSettings,
            ISettingService settingService,
            IStoreService storeService)
        {
            _commonSettings = commonSettings;
            _settingService = settingService;
            _storeService = storeService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {

            //action displaying notification (warning) to a store owner that "limit per store" feature is ignored

            //default setting
            var enabled = _commonSettings.IgnoreStoreLimitations;
            if (!enabled)
            {
                //overridden settings
                var stores = await _storeService.GetAllStoresAsync();
                foreach (var store in stores)
                {
                    var commonSettings = await _settingService.LoadSettingAsync<CommonSettings>(store.Id);
                    enabled = commonSettings.IgnoreStoreLimitations;

                    if (enabled)
                        break;
                }
            }

            //This setting is disabled. No warnings.
            if (!enabled)
                return Content("");

            return View();
        }
    }
}
