using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Common;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using Nop.Web.Framework.Components;

namespace Nop.Web.Areas.Admin.Components
{
    public class AclDisabledWarningViewComponent : NopViewComponent
    {
        private readonly CommonSettings _commonSettings;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;

        public AclDisabledWarningViewComponent(CommonSettings commonSettings,
            ISettingService settingService,
            IStoreService storeService)
        {
            _commonSettings = commonSettings;
            _settingService = settingService;
            _storeService = storeService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            //action displaying notification (warning) to a store owner that "ACL rules" feature is ignored

            //default setting
            var enabled = _commonSettings.IgnoreAcl;
            if (!enabled)
            {
                //overridden settings
                var stores = await _storeService.GetAllStoresAsync();
                foreach (var store in stores)
                {
                    var commonSettings = await _settingService.LoadSettingAsync<CommonSettings>(store.Id);
                    enabled = commonSettings.IgnoreAcl;

                    if (enabled)
                        break;
                }
            }

            //This setting is disabled. No warnings.
            if (!enabled)
                return Content(string.Empty);

            return View();
        }
    }
}
