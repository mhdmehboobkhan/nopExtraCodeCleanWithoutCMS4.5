using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.ZsWebApi.Infrastructure;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Plugins;
using Nop.Web.Framework;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.ZsWebApi
{
    /// <summary>
    /// PLugin
    /// </summary>
    public class ZsWebApiPlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        public ZsWebApiPlugin(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
        }

        public async Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            var mainMenuItem = new SiteMapNode()
            {
                SystemName = "ZsWebApi",
                Title = await _localizationService.GetResourceAsync("Plugins.Misc.ZsWebApi"),
                ControllerName = "",
                ActionName = "",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", AreaNames.Admin } },
            };

            var listMenus = new List<SiteMapNode>();
            var menuItem = new SiteMapNode();

            menuItem = new SiteMapNode()
            {
                SystemName = "ConfigureZsWebApi",
                Title = await _localizationService.GetResourceAsync("Plugins.Misc.ZsWebApi.Configure"),
                ControllerName = "ZsWebApi",
                ActionName = "Configure",
                IconClass = "far fa-dot-circle",
                Visible = true,
                RouteValues = new RouteValueDictionary() { { "area", AreaNames.Admin } },
            };
            listMenus.Add(menuItem);

            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == mainMenuItem.SystemName);
            if (pluginNode != null)
            {
                foreach (var item in listMenus)
                    pluginNode.ChildNodes.Add(item);
            }
            else
            {
                foreach (var item in listMenus)
                    mainMenuItem.ChildNodes.Add(item);
                rootNode.ChildNodes.Add(mainMenuItem);
            }
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/ZsWebApi/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            //settings
            var settings = new ZsWebApiSettings
            {
                Enable = true,
                NSTKey = "bm9wU3RhdGlvblRva2Vu",
                NSTSecret = "bm9wS2V5"
            };

            var payload = new Dictionary<string, object>
                {
                    { PluginDefaults.NST_KEY, settings.NSTKey }
                };

            var token = JwtHelper.JwtEncoder.Encode(payload, settings.NSTSecret);
            settings.NSTToken = token;
            await _settingService.SaveSettingAsync(settings);

            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Misc.ZsWebApi"] = "Zs web api",
                ["Plugins.Misc.ZsWebApi.Configure"] = "Configure",
                ["Plugins.Misc.ZsWebApi.Enable"] = "Enable",
                ["Plugins.Misc.ZsWebApi.Enable.Hint"] = "Mark the plugin enable to work with APi's.",
                ["Plugins.Misc.ZsWebApi.NSTKey"] = "NST key",
                ["Plugins.Misc.ZsWebApi.NSTKey.Hint"] = "Set the nst key to authenticate APi's.",
                ["Plugins.Misc.ZsWebApi.NSTSecret"] = "NST Secret",
                ["Plugins.Misc.ZsWebApi.NSTSecret.Hint"] = "Set the nst secret to authenticate APi's.",
                ["Plugins.Misc.ZsWebApi.NSTToken"] = "NST Token",
                ["Plugins.Misc.ZsWebApi.NSTToken.Hint"] = "Use this token to authenticate APi's.",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<ZsWebApiSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.ZsWebApi");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => false;
    }
}